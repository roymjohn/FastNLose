using System;
using FastNLose.ViewModels;

namespace FastNLose;

public partial class MainPage : ContentPage
{
    MainPageViewModel vm;

    public MainPage()
    {
        InitializeComponent();
        vm = new MainPageViewModel();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // When page becomes visible, refresh categories to ensure UI reflects current time-based state
        vm.RefreshCategories();
    }

    // Called by App when app resumes or is activated
    public void ForceDailyRefresh()
    {
        MainThread.BeginInvokeOnMainThread(() => vm.RefreshCategories());
    }

    int _titleTapCount = 0;
    DateTime _firstTapTime;

    void OnTitleTapped(object sender, EventArgs e)
    {
        var now = DateTime.Now;
        if (_titleTapCount == 0)
            _firstTapTime = now;

        if ((now - _firstTapTime).TotalSeconds > 5)
        {
            // reset if taps spread over more than 5 seconds
            _titleTapCount = 0;
            _firstTapTime = now;
        }

        _titleTapCount++;
        if (_titleTapCount >= 5)
        {
            _titleTapCount = 0;
            ConfirmStartDateShift();
        }
    }

    async void ConfirmStartDateShift()
    {
        var today = DateTime.Today;
        var ok = await DisplayAlert("Confirm", $"Set start date to {today:yyyy-MM-dd} and clear all data?", "Yes", "No");
        if (!ok) return;

        // call into viewmodel to set new start date and clear db/preferences
        vm.SetStartDateAndClear(today);
    }

    void SlotTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is SlotViewModel slot)
            slot.Toggle(vm.SelectedDate);
    }

    void OnPickerSelectionChanged(object sender, EventArgs e)
    {
        if (sender is Picker p && p.BindingContext is SectionViewModel sv)
        {
            // Ignore transient null SelectedItem events which happen during binding updates
            if (p.SelectedItem == null)
                return;

            // Read selected item string
            var sel = p.SelectedItem.ToString();

            // If value didn't change, ignore to avoid duplicate processing
            if (string.Equals(sv.SelectedValue, sel, StringComparison.InvariantCulture))
                return;

            // Assign through the viewmodel property. The property will persist when appropriate.
            System.Diagnostics.Debug.WriteLine($"Picker changed: section={sv.Key} selected='{sel}' date={vm.SelectedDate:yyyy-MM-dd}");
            sv.SelectedValue = sel;
            System.Diagnostics.Debug.WriteLine($"Section SelectedValue set: section={sv.Key} value='{sv.SelectedValue}'");
            // Do not call sv.Save here — the SelectedValue setter already saves when not suppressed.
        }
    }
}
