using FastNLose.Models;
using FastNLose.Services;
using FastNLose.ViewModels;
using Microsoft.Maui.Dispatching;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace FastNLose.Pages;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _db;
    private TrackingItem _currentTracking;
    private ObservableCollection<TrackingItem> _recentItems = new();
    private readonly MainPageViewModel _vm;

    private bool _isTracking;
    private int _tapCount = 0;
    private DateTime _firstTapTime = DateTime.MinValue;

    private IDispatcherTimer _dailyTimer;



    public MainPage()
    {
        InitializeComponent();

        _vm = new MainPageViewModel();
        BindingContext = _vm;

        _db = App.Database;

        LastTwoList.ItemsSource = _recentItems;

        Device.StartTimer(TimeSpan.FromSeconds(30), () =>
        {
            _vm.RefreshAllSlots();
            return true; // keep running
        });
    }

    

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        // Initialize VM (loads score and daily states)
        await _vm.InitializeAsync();

        // Restore any active fasting session
        _currentTracking = await _db.GetActiveTrackingItemAsync();
        if (_currentTracking != null)
        {
            _isTracking = true;
            _vm.Start(_currentTracking.StartTime);
        }
        if (_dailyTimer == null)
        {
            _dailyTimer = Dispatcher.CreateTimer();
            _dailyTimer.Interval = TimeSpan.FromSeconds(30); // checks every 15 sec

            _dailyTimer.Tick += (s, e) =>
            {
                _vm.RefreshAllSlots();


            };
        }
        _dailyTimer?.Start();
        

        await LoadLastTwoRecords();

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _dailyTimer?.Stop();
    }

    public void ForceDailyRefresh()
    {
        _vm.RefreshAllSlots();
    }

    #region Start / Stop / Pick

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (!_isTracking)
        {
            _currentTracking = new TrackingItem
            {
                StartTime = DateTime.Now,
                Date = DateTime.Now.ToString("yyyy-MM-dd")
            };

            await _db.SaveTrackingItemAsync(_currentTracking);

            _isTracking = true;
            _vm.Start(_currentTracking.StartTime);
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        if (!_isTracking)
            return;

        if (_currentTracking == null)
        {
            _currentTracking = await _db.GetActiveTrackingItemAsync();
            if (_currentTracking == null)
            {
                await DisplayAlert("Error", "No active session found.", "OK");
                return;
            }
        }

        _currentTracking.EndTime = DateTime.Now;
        await _db.UpdateTrackingItemAsync(_currentTracking);

        var settings = await _db.GetSettingsAsync() ?? new Settings();
        settings.Score += 10; // or formula you prefer
        await _db.SaveSettingsAsync(settings);

        _isTracking = false;
        _vm.Stop();

        // show the final elapsed for the stopped session
        var span = _currentTracking.EndTime.Value - _currentTracking.StartTime;
        _vm.ElapsedTime = span.ToString(@"dd\:hh\:mm\:ss");

        // cleanup short sessions (your rule)
        await _db.DeleteShortSessionsBeforeLastAsync(_currentTracking.Id);

        // clear saved water states for this session (persist only for current session)
        var key = $"water_{_currentTracking.StartTime.Ticks}";
        Preferences.Remove(key);

        await LoadLastTwoRecords();

        // clear current tracking reference
        _currentTracking = null;
    }

    private async void OnPickTimeClicked(object sender, EventArgs e)
    {
        if (_isTracking)
        {
            await DisplayAlert("Error", "Stop your current session before picking a manual time.", "OK");
            return;
        }

        string action = await DisplayActionSheet("Pick time for:", "Cancel", null, "Start", "Stop");

        if (action == "Start")
        {
            var time = await PickManualTime();
            if (time != null)
            {
                _currentTracking = new TrackingItem
                {
                    StartTime = time.Value,
                    Date = time.Value.ToString("yyyy-MM-dd")
                };
                await _db.SaveTrackingItemAsync(_currentTracking);

                _isTracking = true;
                _vm.Start(_currentTracking.StartTime);
            }
        }
        else if (action == "Stop")
        {
            var time = await PickManualTime();
            if (time != null && _currentTracking != null)
            {
                _currentTracking.EndTime = time.Value;
                await _db.UpdateTrackingItemAsync(_currentTracking);

                _isTracking = false;
                _vm.Stop();
                var span = _currentTracking.EndTime.Value - _currentTracking.StartTime;
                _vm.ElapsedTime = span.ToString(@"dd\:hh\:mm\:ss");

                await DisplayAlert("Saved", "Manual stop recorded!", "OK");
                await LoadLastTwoRecords();

                // clear saved water states for this session
                var key = $"water_{_currentTracking.StartTime.Ticks}";
                Preferences.Remove(key);

                _currentTracking = null;
            }
        }
    }

    private async Task<DateTime?> PickManualTime()
    {
        var tcs = new TaskCompletionSource<DateTime?>();
        var timePicker = new TimePicker { Time = DateTime.Now.TimeOfDay };

        var okButton = new Button { Text = "OK" };
        var cancelButton = new Button { Text = "Cancel" };

        var layout = new StackLayout
        {
            Padding = 20,
            Spacing = 15,
            Children =
            {
                new Label { Text = "Select Time:", FontAttributes = FontAttributes.Bold },
                timePicker,
                new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10,
                    Children = { okButton, cancelButton }
                }
            }
        };

        var modalPage = new ContentPage { Content = layout, BackgroundColor = Colors.White };

        okButton.Clicked += async (s, e) =>
        {
            DateTime selected = DateTime.Today.Add(timePicker.Time);
            tcs.TrySetResult(selected);
            await Navigation.PopModalAsync();
        };

        cancelButton.Clicked += async (s, e) =>
        {
            tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        };

        await Navigation.PushModalAsync(modalPage);
        return await tcs.Task;
    }

    #endregion

    #region Recent records loading

    private async Task LoadLastTwoRecords()
    {
        var items = await _db.GetTrackingItemsAsync();
        var recent = items.OrderByDescending(i => i.StartTime)
                          .Take(_vm.NumberOfSessions)
                          .ToList();

        _recentItems.Clear();
        foreach (var item in recent)
            _recentItems.Add(item);

        // update total so far and days
        _vm.TotalSoFar = TimeSpan.FromTicks(items.Where(i => i.EndTime != null).Sum(i => (i.EndTime - i.StartTime)?.Ticks ?? 0));
        _vm.DaysCount = items.Count > 0 ? (DateTime.Today - items.Min(i => i.StartTime.Date)).Days + 1 : 0;
    }

    #endregion

    #region Clear (triple-tap from title triggers this)

    private async Task OnClearConfirmedAsync()
    {
        // Clear database rows but keep DB file (recommended)
        await _db.ClearAllAsync();

        // Remove daily preferences for today's schedules
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        Preferences.Remove("H2O_" + today);
        Preferences.Remove("WKT_" + today);
        Preferences.Remove("DailyReset");
        Preferences.Remove("WaterLastResetDate");
        Preferences.Remove("ActiveSessionStart");

        // Reset VM UI state
        await DisplayAlert("Done", "All history cleared.", "OK");

        _recentItems.Clear();
        _vm.ElapsedTime = "00:00:00";
        _vm.TotalElapsedTime = "00:00:00";
        _vm.DaysCount = 0;
        _vm.Score = 0;
        _isTracking = false;
        _currentTracking = null;

        // Re-init VM daily states
        await _vm.InitializeAsync();
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        bool result = await DisplayAlert("Confirm", "Clear all data?", "OK", "Cancel");
        if (!result)
            return;

        await OnClearConfirmedAsync();
    }

    private void OnTitleTapped(object sender, EventArgs e)
    {
        var now = DateTime.Now;

        // If this is the first tap of a sequence
        if (_tapCount == 0)
        {
            _firstTapTime = now;
            _tapCount = 1;
            return;
        }

        // If within 5 seconds of the first tap → count it
        if ((now - _firstTapTime).TotalSeconds <= 5)
        {
            _tapCount++;

            if (_tapCount == 3)
            {
                _tapCount = 0; // reset

                // Ask for confirmation and clear
                 OnClearClicked(sender, e);
            }
        }
        else
        {
            // Too slow → reset and start over
            _tapCount = 1;
            _firstTapTime = now;
        }
    }

    #endregion

    #region H2O click handlers (9)

    private void OnH2O1(object sender, EventArgs e) => _vm.ToggleH2O(0);
    private void OnH2O2(object sender, EventArgs e) => _vm.ToggleH2O(1);
    private void OnH2O3(object sender, EventArgs e) => _vm.ToggleH2O(2);
    private void OnH2O4(object sender, EventArgs e) => _vm.ToggleH2O(3);
    private void OnH2O5(object sender, EventArgs e) => _vm.ToggleH2O(4);
    private void OnH2O6(object sender, EventArgs e) => _vm.ToggleH2O(5);
    private void OnH2O7(object sender, EventArgs e) => _vm.ToggleH2O(6);
    private void OnH2O8(object sender, EventArgs e) => _vm.ToggleH2O(7);
    private void OnH2O9(object sender, EventArgs e) => _vm.ToggleH2O(8);

    #endregion

    #region WKT click handlers (9)

    private void OnWKT1(object sender, EventArgs e) => _vm.ToggleWKT(0);
    private void OnWKT2(object sender, EventArgs e) => _vm.ToggleWKT(1);
    private void OnWKT3(object sender, EventArgs e) => _vm.ToggleWKT(2);
    private void OnWKT4(object sender, EventArgs e) => _vm.ToggleWKT(3);
    private void OnWKT5(object sender, EventArgs e) => _vm.ToggleWKT(4);
    private void OnWKT6(object sender, EventArgs e) => _vm.ToggleWKT(5);
    private void OnWKT7(object sender, EventArgs e) => _vm.ToggleWKT(6);
    private void OnWKT8(object sender, EventArgs e) => _vm.ToggleWKT(7);
    private void OnWKT9(object sender, EventArgs e) => _vm.ToggleWKT(8);

    #endregion
}
