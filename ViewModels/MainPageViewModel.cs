
using FastNLose.Models;
using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Globalization;
using FastNLose.ViewModels;

namespace FastNLose.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DateItem> VisibleDates { get; } = new();

    public ObservableCollection<SectionViewModel> Categories { get; }
    = new();

    private DateTime _baseDate;

    private DateTime _selectedDate;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate == value)
                return;

            _selectedDate = value;
            OnPropertyChanged();

            UpdateDateSelection();
            LoadCategoriesForDate(_selectedDate.Date);
        }
    }

    void EvaluateStepsCompletion(DateTime date)
    {
        // Forward to SectionViewModel when this instance represents STEPS
        // Find the section with Key == "STEPS" in Categories (if available)
        var stepsSection = Categories.FirstOrDefault(c => c.Key == "STEPS");
        if (stepsSection != null)
        {
            stepsSection.EvaluateStepsCompletion(date);
        }
    }

    // Public method to refresh current categories for the selected date.
    public void RefreshCategories()
    {
        // Reload data for each existing category so slot states are recalculated against DateTime.Now
        foreach (var c in Categories)
        {
            c.Load(SelectedDate);
        }

        UpdateAllCompleted();
    }

    void ClearPrefsBefore(DateTime cutoff)
    {
        var keys = new[] { "WATER", "STEPS", "WORKOUT", "WALK", "BAN", "SUGAR_8AM", "WEIGHT" };
        // remove entries for the last 365 days before cutoff
        for (int d = 0; d < 365; d++)
        {
            var dt = cutoff.AddDays(-d - 1);
            foreach (var k in keys)
            {
                Preferences.Remove($"{k}_{dt:yyyyMMdd}");
            }
        }
        

    }

    public SectionViewModel Water { get; }
    public SectionViewModel Steps { get; }
    public SectionViewModel Workout { get; }
    public SectionViewModel Walk { get; }
    public SectionViewModel Evils { get; }

    private bool _isAllCompleted;
    public bool IsAllCompleted
    {
        get => _isAllCompleted;
        set { _isAllCompleted = value; OnPropertyChanged(); }
    }

    private const int PageSize = 4;

    private DateTime _startDate = DateTime.Today;

    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (_startDate != value)
            {
                _startDate = value;
                OnPropertyChanged();
                RefreshVisibleDates();
                UpdateCanPrev();
            }
        }
        }

    private const int DaysToShow = 4;

    private void RefreshVisibleDates()
    {
        VisibleDates.Clear();

        for (int i = 0; i < PageSize; i++)
        {
            VisibleDates.Add(new DateItem
            {
                Date = StartDate.AddDays(i)
            });
        }

        // Select today if visible, otherwise auto-select first date
        var today = DateTime.Today;
        var todayItem = VisibleDates.FirstOrDefault(d => d.Date.Date == today);
        var toSelect = todayItem != null ? todayItem.Date : VisibleDates.FirstOrDefault().Date;
        // set SelectedDate via property to update selection UI
        SelectedDate = toSelect;
        // ensure start date is first visible date
        if (_baseDate > VisibleDates[0].Date)
            StartDate = _baseDate;
        UpdateCanPrev();
    }

    void UpdateCanPrev()
    {
        CanPrev = VisibleDates.Count > 0 && StartDate > _baseDate;
        // notify command can execute changed
        (PrevDatesCommand as Command)?.ChangeCanExecute();
    }

    private void UpdateDateSelection()
    {
        foreach (var d in VisibleDates)
            d.IsSelected = (d.Date.Date == SelectedDate.Date);
    }

    public ICommand SelectDateCommand => new Command<DateItem>(date =>
    {
        SelectedDate = date.Date;
    });

    public ICommand SlotTapCommand => new Command<SlotViewModel>(slot =>
    {
        slot.Toggle(SelectedDate);
    });


    public ICommand NextDatesCommand { get; private set; }
    public ICommand PrevDatesCommand { get; private set; }

    bool _canPrev;
    public bool CanPrev
    {
        get => _canPrev;
        set { if (_canPrev == value) return; _canPrev = value; OnPropertyChanged(); }
    }

    void BuildVisibleDates()
    {
        VisibleDates.Clear();
        for (int i = 0; i < PageSize; i++)
        {
            var d = StartDate.AddDays(i);
            VisibleDates.Add(new DateItem
            {
                Date = d,
  
            });
        }
    }

    public MainPageViewModel()
    {
        _baseDate = Preferences.Get("START_DATE", DateTime.Today);
        InitDates(_baseDate);

        Water = SectionViewModel.Create("WATER", 8, 6, 2, 6);
        // create Steps picker backing VM so LoadDay can use it; items same as CategoryFactory
        Steps = SectionViewModel.Create("STEPS", 0, 0, 1, 0);
        Steps.IsPicker = true;
        var stepsItems = new List<string>();
        for (int v = 5000; v <= 12000; v += 500)
            stepsItems.Add(v.ToString());
        Steps.Items = stepsItems;
        Workout = SectionViewModel.Create("WORKOUT", 6, 6, 3, 4);
        Workout.Title = "WORKOUT-5m";
        Walk = SectionViewModel.Create("WALK", 6, 6, 3, 6);
        Walk.Title = "WALK-RUN-5m";
        Evils = SectionViewModel.Create("EVILS", 4, 6, 5, 4, true);

        // Initialize visible dates and select today if present
        RefreshVisibleDates();

        NextDatesCommand = new Command(() => StartDate = StartDate.AddDays(PageSize));
        PrevDatesCommand = new Command(() => StartDate = StartDate.AddDays(-PageSize), () => CanPrev);
        UpdateCanPrev();
    }

    public void SetStartDateAndClear(DateTime newStart)
    {
        // save new start date
        Preferences.Set("START_DATE", newStart);
        _baseDate = newStart;

        // clear prefs before new start
        ClearPrefsBefore(newStart);

        // Update visible dates to not show dates before base
        InitDates(newStart);
        RefreshVisibleDates();
    }

    void InitDates(DateTime start)
    {
        VisibleDates.Clear();
        for (int i = 0; i < PageSize; i++)
            VisibleDates.Add(new DateItem
            {
                Date = start.AddDays(i),

            });
    }

    public void MoveDates(int delta)
    {
        var newStart = VisibleDates[0].Date.AddDays(delta);
        if (newStart < _baseDate) return;
        InitDates(newStart);
        SelectedDate = VisibleDates[0].Date;
    }

    void LoadDay(DateTime date)
    {
        Water.Load(date);
        Steps.Load(date);
        Workout.Load(date);
        Walk.Load(date);
        Evils.Load(date);

        IsAllCompleted =
            Water.IsCompleted &&
            Steps.IsCompleted &&
            Workout.IsCompleted &&
            Walk.IsCompleted &&
            Evils.IsCompleted;
    }

    private void LoadCategoriesForDate(DateTime date)
    {
        // Unsubscribe from previous category change notifications
        foreach (var c in Categories.ToList())
            c.PropertyChanged -= Category_PropertyChanged;

        Categories.Clear();

        // Build and subscribe to new categories
        void addAndSubscribe(SectionViewModel s)
        {
            Categories.Add(s);
            s.PropertyChanged += Category_PropertyChanged;
        }

        addAndSubscribe(CategoryFactory.BuildWater(date));
        addAndSubscribe(CategoryFactory.BuildWorkout(date));
        addAndSubscribe(CategoryFactory.BuildWalk(date));
        addAndSubscribe(CategoryFactory.BuildYes(date));
        addAndSubscribe(CategoryFactory.BuildBan(date));
        addAndSubscribe(CategoryFactory.BuildYesNo("16Hr Fast", date));
        addAndSubscribe(CategoryFactory.BuildWeight(date));
        addAndSubscribe(CategoryFactory.BuildSugar8AM(date));
        // Steps picker section placed at the end
        addAndSubscribe(CategoryFactory.BuildSteps(date));

        UpdateAllCompleted();
    }

    void Category_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Any meaningful change in a section (Background/IsCompleted) should re-evaluate overall completion
        UpdateAllCompleted();
    }

    private void UpdateAllCompleted()
    {
        // If there are no categories treat as not completed.
        IsAllCompleted = Categories.Any() && Categories.All(c => c.IsCompleted);
        OnPropertyChanged(nameof(IsAllCompleted));
    }


    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/* ---------------- SECTION VM ---------------- */

public class SectionViewModel : INotifyPropertyChanged
{
    public ObservableCollection<SlotViewModel> Slots { get; } = new();
    public string Key { get; private set; }
    // Title shown in UI; can be friendly text independent of storage Key
    public string Title { get; set; }
    public string Name => Title ?? Key;
    // Controls whether the layout shows title above controls
    public bool ForceStacked { get; set; }
        // whether this section can be edited for the currently loaded date
        public bool IsEditable { get; private set; }
    int _requiredGreen;
    DateTime? _loadedDate;
    bool _suppressSave;
    // spacing between slot items in the UI (pixels)
    public double SlotItemSpacing { get; set; } = 7;
    bool _isFailed;
    public bool IsFailed
    {
        get => _isFailed;
        private set { if (_isFailed == value) return; _isFailed = value; OnPropertyChanged(); }
    }

    // If true this section is represented by a Picker (single selection)
    public bool IsPicker { get; set; }
    public IList<string> Items { get; set; }
    string _selectedValue;
    public string SelectedValue
    {
        get => _selectedValue;
        set
        {
            if (_selectedValue == value) return;
            _selectedValue = value;
            OnPropertyChanged();
            // Save when not suppressed (i.e. when change is from the user)
            if (!_suppressSave && _loadedDate.HasValue)
                Save(_loadedDate.Value);

            // Special logic for SUGAR_8AM and WEIGHT when determining completion color
            if ((_loadedDate.HasValue) && (Key == "SUGAR_8AM" || Key == "WEIGHT"))
            {
                try
                {
                    var today = _loadedDate.Value.Date;
                    
                    // For future dates (tomorrow onwards), always mark as not completed
                    if (today > DateTime.Today)
                    {
                        IsCompleted = false;
                        return;
                    }
                    
                    // If no value selected, mark as not completed
                    if (string.IsNullOrEmpty(_selectedValue))
                    {
                        IsCompleted = false;
                        return;
                    }
                    
                    if (!double.TryParse(_selectedValue, out var currVal))
                    {
                        IsCompleted = false;
                        return;
                    }

                    // Get previous 3 days values
                    var prevValues = new List<double>();
                    for (int i = 1; i <= 3; i++)
                    {
                        var d = today.AddDays(-i);
                        var prevStr = Preferences.Get($"{Key}_{d:yyyyMMdd}", string.Empty);
                        if (!string.IsNullOrEmpty(prevStr) && double.TryParse(prevStr, out var prevVal))
                        {
                            prevValues.Add(prevVal);
                        }
                    }

                    // If we have previous values, compare against their average
                    if (prevValues.Count > 0)
                    {
                        var avg = prevValues.Average();
                        // Completed (green) if current is lower than average
                        IsCompleted = currVal < avg;
                    }
                    else
                    {
                        // No previous data (first day): mark as completed (green) when value is selected
                        IsCompleted = true;
                    }
                }
                catch
                {
                    IsCompleted = false;
                }
            }
        }
    }

    // If true this section is represented by a toggle (Switch)
    public bool IsToggle { get; set; }
    bool _toggleValue;
    public bool ToggleValue
    {
        get => _toggleValue;
        set
        {
            if (_toggleValue == value) return;
            _toggleValue = value;
            OnPropertyChanged();
            // Persist toggle and update completion state for toggle sections
            Save();
            if (_loadedDate.HasValue)
            {
                // For toggle sections, consider completion when true
                IsCompleted = _toggleValue;
            }
        }
    }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        private set
        {
            if (_isCompleted == value) return;
            _isCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Background));
            OnPropertyChanged(nameof(TextColor));
        }
    }

    public Color Background => IsCompleted ? Color.FromArgb("#90EE90") : (IsFailed ? Color.FromArgb("#FFCDD2") : Colors.Transparent);
    public Color TextColor => IsCompleted ? Colors.Black : Colors.White;

    public static SectionViewModel Create(string key, int count, int startHour, int interval, int required, bool rectangle = false)
    {
        var vm = new SectionViewModel { Key = key, _requiredGreen = required };
        vm.Title = key.Replace('_', ' ');
        for (int i = 0; i < count; i++)
            vm.Slots.Add(new SlotViewModel(startHour + i * interval, rectangle, vm));
        // Force stacked layout for these multi-slot sections
        if (key == "WATER" || key == "STEPS" || key == "WORKOUT" || key == "WALK" || key == "BAN")
            vm.ForceStacked = true;
        return vm;
    }

    public static SectionViewModel CreateLabeled(string key, string[] labels)
    {
        var vm = new SectionViewModel { Key = key, _requiredGreen = labels.Length };
        vm.Title = key.Replace('_', ' ');
        // Force stacked layout for labeled sections (like BAN)
        if (key == "BAN")
            vm.ForceStacked = true;
        foreach (var lab in labels)
        {
            var s = new SlotViewModel(0, true, vm) { Label = ToCamelCase(lab) };
            vm.Slots.Add(s);
        }
        return vm;
    }

    static string ToCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        var lower = input.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
    }

    public void Load(DateTime date)
    {
        _loadedDate = date;

        // editable only for today or past dates
        IsEditable = date.Date <= DateTime.Today;
        OnPropertyChanged(nameof(IsEditable));

        if (Slots.Count > 0)
        {
            var json = Preferences.Get($"{Key}_{date:yyyyMMdd}", "");
            int[] states = string.IsNullOrEmpty(json)
                ? new int[Slots.Count]
                : JsonSerializer.Deserialize<int[]>(json);

            for (int i = 0; i < Slots.Count; i++)
                Slots[i].SetState(states[i], date);

            UpdateCompletion();
        }
        else if (IsPicker)
        {
            var val = Preferences.Get($"{Key}_{date:yyyyMMdd}", string.Empty);
            Debug.WriteLine($"Load picker: key={Key}_{date:yyyyMMdd} value='{val}' itemsCount={(Items?.Count ?? 0)}");
            _suppressSave = true;
            string chosen = null;
            if (!string.IsNullOrEmpty(val))
            {
                if (Items != null && Items.Count > 0)
                {
                    // exact match
                    var idx = Items.IndexOf(val);
                    if (idx >= 0)
                        chosen = Items[idx];
                    else
                    {
                        // case-insensitive match
                        var match = Items.FirstOrDefault(it => string.Equals(it, val, StringComparison.InvariantCultureIgnoreCase));
                        if (match != null)
                            chosen = match;
                        else
                        {
                            // try numeric match (handles culture/format differences)
                            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                            {
                                var numMatch = Items.FirstOrDefault(it => double.TryParse(it, NumberStyles.Any, CultureInfo.InvariantCulture, out var itv) && Math.Abs(itv - v) < 0.0001);
                                if (numMatch != null)
                                    chosen = numMatch;
                            }
                        }
                    }
                }
                // fallback to raw val if no chosen found
                if (chosen == null)
                    chosen = val;
            }
            else if (Items != null && Items.Count > 0)
            {
                chosen = string.Empty;
            }

            // Apply chosen value through the property so completion logic runs.
            _suppressSave = true;
            SelectedValue = chosen;
            _suppressSave = false;
            // Special handling for STEPS picker: evaluate against previous 3 days
            if (Key == "STEPS")
            {
                EvaluateStepsCompletion(date);
            }
        }
        else if (IsToggle)
        {
            var val = Preferences.Get($"{Key}_{date:yyyyMMdd}", "0");
            ToggleValue = val == "1";
        }
    }

    public void Save(DateTime date)
    {
        if (Slots.Count > 0)
        {
            Preferences.Set($"{Key}_{date:yyyyMMdd}",
                JsonSerializer.Serialize(Slots.Select(s => s.State)));
            Debug.WriteLine($"Save slots: key={Key}_{date:yyyyMMdd}");
        }
        else if (IsPicker)
        {
            if (SelectedValue != null)
            {
                Preferences.Set($"{Key}_{date:yyyyMMdd}", SelectedValue ?? string.Empty);
                Debug.WriteLine($"Save picker: key={Key}_{date:yyyyMMdd} value='{SelectedValue ?? string.Empty}'");
                if (Key == "STEPS")
                {
                    // Re-evaluate step completion after save
                    EvaluateStepsCompletion(date);
                }
            }
        }
        else if (IsToggle)
        {
            Preferences.Set($"{Key}_{date:yyyyMMdd}", ToggleValue ? "1" : "0");
        }
    }

    // Save using the last loaded date
    public void Save()
    {
        if (_loadedDate.HasValue)
            Save(_loadedDate.Value);
    }

    // Evaluate steps for this section if key is STEPS
    public void EvaluateStepsCompletion(DateTime date)
    {
        if(date > DateTime.Today)
        {
            // Don't evaluate future dates; treat as not completed without failure
            IsCompleted = false;
            IsFailed = false;
            return;
        }

        if (Key != "STEPS") return;
        try
        {
            var key = $"{Key}_{date:yyyyMMdd}";
            var todayStr = Preferences.Get(key, string.Empty);
            if (!int.TryParse(todayStr, out var todaySteps))
            {
                IsFailed = false;
                IsCompleted = false;
                return;
            }

            var prevValues = new List<int>();
            for (int i = 1; i <= 3; i++)
            {
                var d = date.AddDays(-i);
                var s = Preferences.Get($"{Key}_{d:yyyyMMdd}", string.Empty);
                if (int.TryParse(s, out var v)) prevValues.Add(v);
            }

            // Compute average of up to 3 previous days; if none available average is 0
            var avg = prevValues.Count > 0 ? (int)Math.Round(prevValues.Average()) : 0;
            if (avg == 0 || todaySteps > avg)
            {
                IsCompleted = true;
                IsFailed = false;
            }
            else if (todaySteps < avg)
            {
                IsCompleted = false;
                IsFailed = true;
            }
            else
            {
                IsCompleted = false;
                IsFailed = false;
            }
        }
        catch
        {
        }
    }

    internal void UpdateCompletion()
    {
        IsCompleted = Slots.Count(s => s.State == 2) >= _requiredGreen;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/* ---------------- SLOT VM ---------------- */

public class SlotViewModel : INotifyPropertyChanged
{
    public int Hour { get; }
    public int State { get; private set; }   // 0 yellow, 1 red, 2 green
    readonly bool _rectangle;
    readonly SectionViewModel _parent;

    public bool IsClickable => State == 1;

    public Color Color => State switch
    {
        0 => Color.FromArgb("#FFF59D"), // lighter yellow
        1 => Colors.Red,
        _ => Colors.Green
    };

    public int CornerRadius => _rectangle ? 4 : 10;

    // Display value used by the UI. For now show the hour.
    public string Label { get; set; }
    public string Value => !string.IsNullOrEmpty(Label) ? Label : Hour.ToString();

    public SlotViewModel(int hour, bool rectangle, SectionViewModel parent)
    {
        Hour = hour;
        _rectangle = rectangle;
        _parent = parent;
    }

    public void SetState(int saved, DateTime date)
    {
        var slotTime = date.AddHours(Hour);
        State = (DateTime.Now >= slotTime && saved == 0) ? 1 : saved;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(IsClickable));
    }

    public void Toggle(DateTime date)
    {
        // Only allow transitioning red -> green on user tap
        if (State == 1)
        {
            State = 2;
            _parent.Save(date);
            _parent.UpdateCompletion();
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(IsClickable));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
