using FastNLose.Models;
using Microsoft.Maui.Graphics;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Timers;
using Plugin.LocalNotification.AndroidOption;

namespace FastNLose.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        // ************* FASTING TRACKING ORIGINAL LOGIC (UNCHANGED) *************
        private DateTime? _startTime;
        private string _elapsedTime = "00:00:00";
        private string _totalElapsed = "00:00:00";
        private System.Timers.Timer _timer;

        private bool _isRunning;
        private int _daysCount = 0;
        private int _score;
        private const int numberOfSessionsConst = 7;

        private TimeSpan totalSoFar = TimeSpan.Zero;

        public event PropertyChangedEventHandler PropertyChanged;

        public int NumberOfSessions => numberOfSessionsConst;

        public ObservableCollection<DailySummary> Progress { get; set; } = new ObservableCollection<DailySummary>();


        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(); }
        }

        public int DaysCount
        {
            get => _daysCount;
            set { _daysCount = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public string ElapsedTime
        {
            get => _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(); }
        }

        public string TotalElapsedTime
        {
            get => _totalElapsed;
            set { _totalElapsed = value; OnPropertyChanged(); }
        }

        public TimeSpan TotalSoFar
        {
            get => totalSoFar;
            set { totalSoFar = value; OnPropertyChanged(); }
        }

        // ************************************************************************
        // *************   DAILY H2O + WKT SYSTEM (NEW LOGIC)   *******************
        // ************************************************************************

        private const int COUNT = 9;

        private int[] h2oStates = new int[COUNT]; // 0=yellow, 1=red, 2=green
        private int[] wktStates = new int[COUNT];

        private Color[] h2oColors = new Color[COUNT];
        private Color[] wktColors = new Color[COUNT];

        private bool[] h2oEnabled = new bool[COUNT];
        private bool[] wktEnabled = new bool[COUNT];

        // The 9 schedule slot hours: every 2 hrs from 7AM → 23PM
        private readonly int[] slotHours = { 5, 7, 9, 11, 13, 15, 17, 19, 21 };


        // --- Bindable Color Properties (H2O) ---
        public Color H2OColor1 { get => h2oColors[0]; set { h2oColors[0] = value; OnPropertyChanged(); } }
        public Color H2OColor2 { get => h2oColors[1]; set { h2oColors[1] = value; OnPropertyChanged(); } }
        public Color H2OColor3 { get => h2oColors[2]; set { h2oColors[2] = value; OnPropertyChanged(); } }
        public Color H2OColor4 { get => h2oColors[3]; set { h2oColors[3] = value; OnPropertyChanged(); } }
        public Color H2OColor5 { get => h2oColors[4]; set { h2oColors[4] = value; OnPropertyChanged(); } }
        public Color H2OColor6 { get => h2oColors[5]; set { h2oColors[5] = value; OnPropertyChanged(); } }
        public Color H2OColor7 { get => h2oColors[6]; set { h2oColors[6] = value; OnPropertyChanged(); } }
        public Color H2OColor8 { get => h2oColors[7]; set { h2oColors[7] = value; OnPropertyChanged(); } }
        public Color H2OColor9 { get => h2oColors[8]; set { h2oColors[8] = value; OnPropertyChanged(); } }

        // --- Bindable Enabled Properties (H2O) ---
        public bool H2OEnabled1 { get => h2oEnabled[0]; set { h2oEnabled[0] = value; OnPropertyChanged(); } }
        public bool H2OEnabled2 { get => h2oEnabled[1]; set { h2oEnabled[1] = value; OnPropertyChanged(); } }
        public bool H2OEnabled3 { get => h2oEnabled[2]; set { h2oEnabled[2] = value; OnPropertyChanged(); } }
        public bool H2OEnabled4 { get => h2oEnabled[3]; set { h2oEnabled[3] = value; OnPropertyChanged(); } }
        public bool H2OEnabled5 { get => h2oEnabled[4]; set { h2oEnabled[4] = value; OnPropertyChanged(); } }
        public bool H2OEnabled6 { get => h2oEnabled[5]; set { h2oEnabled[5] = value; OnPropertyChanged(); } }
        public bool H2OEnabled7 { get => h2oEnabled[6]; set { h2oEnabled[6] = value; OnPropertyChanged(); } }
        public bool H2OEnabled8 { get => h2oEnabled[7]; set { h2oEnabled[7] = value; OnPropertyChanged(); } }
        public bool H2OEnabled9 { get => h2oEnabled[8]; set { h2oEnabled[8] = value; OnPropertyChanged(); } }


        // --- Bindable Color Properties (WKT) ---
        public Color WKTColor1 { get => wktColors[0]; set { wktColors[0] = value; OnPropertyChanged(); } }
        public Color WKTColor2 { get => wktColors[1]; set { wktColors[1] = value; OnPropertyChanged(); } }
        public Color WKTColor3 { get => wktColors[2]; set { wktColors[2] = value; OnPropertyChanged(); } }
        public Color WKTColor4 { get => wktColors[3]; set { wktColors[3] = value; OnPropertyChanged(); } }
        public Color WKTColor5 { get => wktColors[4]; set { wktColors[4] = value; OnPropertyChanged(); } }
        public Color WKTColor6 { get => wktColors[5]; set { wktColors[5] = value; OnPropertyChanged(); } }
        public Color WKTColor7 { get => wktColors[6]; set { wktColors[6] = value; OnPropertyChanged(); } }
        public Color WKTColor8 { get => wktColors[7]; set { wktColors[7] = value; OnPropertyChanged(); } }
        public Color WKTColor9 { get => wktColors[8]; set { wktColors[8] = value; OnPropertyChanged(); } }

        // --- Bindable Enabled Properties (WKT) ---
        public bool WKTEnabled1 { get => wktEnabled[0]; set { wktEnabled[0] = value; OnPropertyChanged(); } }
        public bool WKTEnabled2 { get => wktEnabled[1]; set { wktEnabled[1] = value; OnPropertyChanged(); } }
        public bool WKTEnabled3 { get => wktEnabled[2]; set { wktEnabled[2] = value; OnPropertyChanged(); } }
        public bool WKTEnabled4 { get => wktEnabled[3]; set { wktEnabled[3] = value; OnPropertyChanged(); } }
        public bool WKTEnabled5 { get => wktEnabled[4]; set { wktEnabled[4] = value; OnPropertyChanged(); } }
        public bool WKTEnabled6 { get => wktEnabled[5]; set { wktEnabled[5] = value; OnPropertyChanged(); } }
        public bool WKTEnabled7 { get => wktEnabled[6]; set { wktEnabled[6] = value; OnPropertyChanged(); } }
        public bool WKTEnabled8 { get => wktEnabled[7]; set { wktEnabled[7] = value; OnPropertyChanged(); } }
        public bool WKTEnabled9 { get => wktEnabled[8]; set { wktEnabled[8] = value; OnPropertyChanged(); } }


        // ***********************************************************************

        public MainPageViewModel()
        {
            InitH2O();
            InitWKT();

            // timer
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) => TimerTick();
        }


        // ******** INITIALIZATION ********

        private void InitH2O()
        {
            for (int i = 0; i < COUNT; i++)
            {
                h2oStates[i] = 0;
                h2oColors[i] = Yellow();
                h2oEnabled[i] = false;
            }
        }

        private void InitWKT()
        {
            for (int i = 0; i < COUNT; i++)
            {
                wktStates[i] = 0;
                wktColors[i] = Yellow();
                wktEnabled[i] = false;
            }
        }

        private Color Yellow() => Color.FromArgb("#FFD54F");


        // ******** PUBLIC INITIALIZER CALLED BY PAGE ********

        public async Task InitializeAsync()
        {
            // Load score
            Score = (await App.Database.GetSettingsAsync())?.Score ?? 0;

            LoadDailyState();

            UpdateDailyButtons();

            BuildProgressTable();

        }


        // ******** FASTING START / STOP (existing behavior preserved) ********

        public void Start(DateTime? startTime = null)
        {
            if (IsRunning) return;

            _startTime = startTime ?? DateTime.Now;
            Preferences.Set("ActiveSessionStart", _startTime.Value.Ticks);

            IsRunning = true;
            _timer.Start();

            UpdateElapsedTime();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _timer.Stop();
            IsRunning = false;

            Preferences.Remove("ActiveSessionStart");

            UpdateElapsedTime();
            BuildProgressTable();

        }


        // ******** TIMER TICK ********

        private void TimerTick()
        {
            UpdateElapsedTime();
            UpdateDailyButtons();
        }


        private void UpdateElapsedTime()
        {
            if (!_startTime.HasValue) return;

            var span = DateTime.Now - _startTime.Value;

            ElapsedTime = span.ToString(@"dd\:hh\:mm\:ss");
            TotalElapsedTime = (TotalSoFar + span).ToString(@"dd\:hh\:mm\:ss");
        }


        // ***********************************************************************
        // ******** DAILY BUTTON LOGIC (H2O + WKT) *******************************
        // ***********************************************************************

        private void UpdateDailyButtons()
        {
            var now = DateTime.Now;

            // Reset if a new day
            var today = now.ToString("yyyy-MM-dd");
            var lastDay = Preferences.Get("DailyReset", "");

            if (today != lastDay)
            {
                InitH2O();
                InitWKT();
                Preferences.Set("DailyReset", today);
            }

            ApplyScheduleRules(h2oStates, h2oColors, h2oEnabled, "H2O");
            ApplyScheduleRules(wktStates, wktColors, wktEnabled, "WKT");

            ApplyToBindableProperties();

            SaveDailyState();
        }


        private void ApplyScheduleRules(int[] states, Color[] colors, bool[] enabled, string groupName)
        {
            var now = DateTime.Now;

            for (int i = 0; i < COUNT; i++)
            {
                int slotHour = slotHours[i];
                var slotTime = new DateTime(now.Year, now.Month, now.Day, slotHour, 0, 0);

                // If current time passed slot and it's still yellow → red
                if (now >= slotTime)
                {
                    if (states[i] == 0)
                    {
                        states[i] = 1;
                        TriggerRedAlert(i, groupName);
                    }
                }

                // Assign colors & enabled states
                if (states[i] == 0)
                {
                    colors[i] = Yellow();
                    enabled[i] = false;
                }
                else if (states[i] == 1)
                {
                    colors[i] = Colors.Red;
                    enabled[i] = true;
                }
                else // green
                {
                    colors[i] = Colors.Green;
                    enabled[i] = false;
                }
            }
        }



        // ******** CLICK HANDLERS FROM UI (only red → green allowed) ********

        public void ToggleH2O(int index)
        {
            if (h2oStates[index] != 1) return; // only red clickable
            h2oStates[index] = 2;
            UpdateDailyButtons();
            BuildProgressTable();

        }

        public void ToggleWKT(int index)
        {
            if (wktStates[index] != 1) return;
            wktStates[index] = 2;
            UpdateDailyButtons();
            BuildProgressTable();

        }


        // ******** PERSISTENCE ********

        private void SaveDailyState()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            Preferences.Set("H2O_" + today, JsonSerializer.Serialize(h2oStates));
            Preferences.Set("WKT_" + today, JsonSerializer.Serialize(wktStates));
        }

        private void LoadDailyState()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            string h2oJson = Preferences.Get("H2O_" + today, "");
            string wktJson = Preferences.Get("WKT_" + today, "");

            if (!string.IsNullOrEmpty(h2oJson))
                h2oStates = JsonSerializer.Deserialize<int[]>(h2oJson) ?? new int[COUNT];

            if (!string.IsNullOrEmpty(wktJson))
                wktStates = JsonSerializer.Deserialize<int[]>(wktJson) ?? new int[COUNT];

            UpdateDailyButtons();
        }


        // ******** PUSH TO BINDABLE PROPERTIES ********

        private void ApplyToBindableProperties()
        {
            // H2O
            H2OColor1 = h2oColors[0]; H2OEnabled1 = h2oEnabled[0];
            H2OColor2 = h2oColors[1]; H2OEnabled2 = h2oEnabled[1];
            H2OColor3 = h2oColors[2]; H2OEnabled3 = h2oEnabled[2];
            H2OColor4 = h2oColors[3]; H2OEnabled4 = h2oEnabled[3];
            H2OColor5 = h2oColors[4]; H2OEnabled5 = h2oEnabled[4];
            H2OColor6 = h2oColors[5]; H2OEnabled6 = h2oEnabled[5];
            H2OColor7 = h2oColors[6]; H2OEnabled7 = h2oEnabled[6];
            H2OColor8 = h2oColors[7]; H2OEnabled8 = h2oEnabled[7];
            H2OColor9 = h2oColors[8]; H2OEnabled9 = h2oEnabled[8];

            // WKT
            WKTColor1 = wktColors[0]; WKTEnabled1 = wktEnabled[0];
            WKTColor2 = wktColors[1]; WKTEnabled2 = wktEnabled[1];
            WKTColor3 = wktColors[2]; WKTEnabled3 = wktEnabled[2];
            WKTColor4 = wktColors[3]; WKTEnabled4 = wktEnabled[3];
            WKTColor5 = wktColors[4]; WKTEnabled5 = wktEnabled[4];
            WKTColor6 = wktColors[5]; WKTEnabled6 = wktEnabled[5];
            WKTColor7 = wktColors[6]; WKTEnabled7 = wktEnabled[6];
            WKTColor8 = wktColors[7]; WKTEnabled8 = wktEnabled[7];
            WKTColor9 = wktColors[8]; WKTEnabled9 = wktEnabled[8];
        }

        public void BuildProgressTable()
        {
            Progress.Clear();

            for (int i = 0; i < 7; i++)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                string key = day.ToString("yyyy-MM-dd");

                // Load H2O
                string h2oJson = Preferences.Get("H2O_" + key, "");
                int[] h2o = string.IsNullOrEmpty(h2oJson) ? new int[9] :
                               (JsonSerializer.Deserialize<int[]>(h2oJson) ?? new int[9]);

                int h2Count = h2o.Count(x => x == 2);   // green only

                // Load WKT
                string wktJson = Preferences.Get("WKT_" + key, "");
                int[] wkt = string.IsNullOrEmpty(wktJson) ? new int[9] :
                               (JsonSerializer.Deserialize<int[]>(wktJson) ?? new int[9]);

                int wktCount = wkt.Count(x => x == 2);

                // Fasting total (load from DB)
                var items = App.Database.GetTrackingItemsAsync().Result;
                var todaysSessions = items
                    .Where(t => t.StartTime.Date == day.Date && t.EndTime != null)
                    .ToList();

                TimeSpan fasting = TimeSpan.Zero;
                foreach (var s in todaysSessions)
                    fasting += (s.EndTime.Value - s.StartTime);

                string fastingStr = $"{(int)fasting.TotalHours:D2}:{fasting.Minutes:D2}";


                Progress.Add(new DailySummary
                {
                    Date = day.ToString("MM/dd"),
                    H2 = h2Count,
                    Wk = wktCount,
                    Fs = fastingStr
                });
            }
        }


        private async void TriggerRedAlert(int index, string group)
        {
            try
            {
                // Vibrate as before
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(120));
            }
            catch { }

            // ---- REAL SYSTEM NOTIFICATION ----
            var request = new NotificationRequest
            {
                NotificationId = 1000 + index + (group == "WKT" ? 100 : 0),
                Title = $"{group} Slot {index + 1}",
                Description = $"Slot {index + 1} is now active.",
                CategoryType = NotificationCategoryType.Reminder,
                Schedule =
        {
            NotifyTime = DateTime.Now // show immediately
        }
            };

            await LocalNotificationCenter.Current.Show(request);
        }

        public void RefreshAllSlots()
        {
            UpdateElapsedTime();  // recalculates red/yellow/green
            UpdateDailyButtons();
        }



        // ******** PROPERTY CHANGED ********

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
    }
}
