using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
                case nameof(settings.On):
                    if (settings.On)
                    {
                        DependencyService.Get<ISetAlarm>().SetAlarm();
                    }
                    else
                    {
                        DependencyService.Get<ISetAlarm>().CancelAlarm();
                    }
                    break;
                case nameof(settings.MinutesInterval):
                    DependencyService.Get<ISetAlarm>().SetAlarm();
                    break;
                case nameof(settings.VibrateLength):
                case nameof(settings.VibrateAmplitude):
                    DependencyService.Get<IVibrate>().Vibrate();
                    break;
                default:
                    break;
            }
            SyncToSettings();
        }

        private void SyncToSettings()
        {
            var settings = ((App)App.Current).Settings;
            startButton.Text = settings.On ? "Click to stop" : "Click to start";
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
