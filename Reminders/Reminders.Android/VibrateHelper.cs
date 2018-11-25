using Android.App;
using Android.Content;
using Android.OS;
using Reminders.Droid;
using System;

[assembly: Xamarin.Forms.Dependency(typeof(VibrateHelper))]
namespace Reminders.Droid
{
    sealed class VibrateHelper : IVibrate
    {
        void IVibrate.Vibrate()
        {
            var settings = ((App)App.Current).Settings;
            var milliseconds = Convert.ToInt32(Math.Round(settings.VibrateLength));
            Vibrate(Application.Context, milliseconds);
        }

        internal static void Vibrate(Context applicationContext, int milliseconds)
        {
            var v = (Vibrator)applicationContext.GetSystemService(Context.VibratorService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                v.Vibrate(VibrationEffect.CreateOneShot(milliseconds, VibrationEffect.DefaultAmplitude));
            }
            else
            {
                v.Vibrate(milliseconds);
            }
            //Android.Widget.Toast.MakeText(applicationContext, "Vibrating", Android.Widget.ToastLength.Short).Show();
        }

        internal static CanVibrateState CanVibrate(Context applicationContext, Settings settings)
        {
            if (settings.MinutesInterval < Settings.minimumInterval)
            {
                return CanVibrateState.InvalidInterval;
            }
            if (!settings.On)
            {
                return CanVibrateState.Stopped;
            }
            if (settings.IgnoreIfBetweenTimes)
            {
                var currentTime = Settings.NextAlarm(settings.MostRecentAlarmAttempt, settings.MinutesInterval).TimeOfDay;
                if (((settings.IgnoreTimeStart > settings.IgnoreTimeEnd)
                        && ((currentTime > settings.IgnoreTimeStart && currentTime <= TimeSpan.MaxValue)
                            || (currentTime >= TimeSpan.MinValue && currentTime < settings.IgnoreTimeEnd)))
                    || (currentTime > settings.IgnoreTimeStart && currentTime < settings.IgnoreTimeEnd))
                {
                    return CanVibrateState.Betweentimes;
                }
            }
            return CanVibrateState.Yes;
        }
    }
}