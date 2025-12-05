using System;
using System.Threading.Tasks;
using FastNLose.Models;
using FastNLose.Services;

namespace FastNLose.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _db;
        private Settings _settings;

        public SettingsPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _settings = await _db.GetSettingsAsync() ?? new Settings();

            targetEntry.Text = _settings.TargetTotalHours > 0 ? _settings.TargetTotalHours.ToString() : string.Empty;
            expectedStartEntry.Text = _settings.ExpectedStart != TimeSpan.Zero ? _settings.ExpectedStart.ToString(@"hh\:mm") : string.Empty;
            expectedStopEntry.Text = _settings.ExpectedStop != TimeSpan.Zero ? _settings.ExpectedStop.ToString(@"hh\:mm") : string.Empty;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                double.TryParse(targetEntry.Text, out double targetHours);
                _settings.TargetTotalHours = targetHours;

                if (TimeSpan.TryParse(expectedStartEntry.Text, out var start))
                    _settings.ExpectedStart = start;

                if (TimeSpan.TryParse(expectedStopEntry.Text, out var stop))
                    _settings.ExpectedStop = stop;

                await _db.SaveSettingsAsync(_settings);
                await DisplayAlert("Saved", "Settings have been saved successfully.", "OK");

                await Shell.Current.GoToAsync(".."); // Navigate back to main page
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save settings: {ex.Message}", "OK");
            }
        }
    }
}
