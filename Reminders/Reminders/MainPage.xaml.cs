using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace Reminders
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = ((App)App.Current).Settings;
            SyncToSettings();
        }

        internal void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settings = ((App)App.Current).Settings;
            switch (e.PropertyName)
            {
                case nameof(settings.MostRecentAlarmAttempt):
                    nextReminderLabel.Text = DependencyService.Get<ISetAlarm>().NextAlarm();
                    return;
                case nameof(settings.VibrateLength):
                    DependencyService.Get<IVibrate>().Vibrate();
                    break;
                default:
                    break;
            }
            SyncToSettings();
        }

        internal void SyncToSettings()
        {
            var settings = ((App)App.Current).Settings;
            if (settings.On)
            {
                startButton.Text = "Click to stop";
                DependencyService.Get<ISetAlarm>().SetAlarm();
            }
            else
            {
                startButton.Text = "Click to start";
                DependencyService.Get<ISetAlarm>().CancelAlarm();
            }
            betweenTimeStart.IsEnabled = settings.IgnoreIfBetweenTimes;
            betweenTimeEnd.IsEnabled = settings.IgnoreIfBetweenTimes;
            nextReminderLabel.Text = DependencyService.Get<ISetAlarm>().NextAlarm();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var settings = ((App)App.Current).Settings;
            settings.On = !settings.On;
        }
    }
}
