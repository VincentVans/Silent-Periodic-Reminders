using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Reminders.Droid;
using System;

[assembly: Xamarin.Forms.Dependency(typeof(VibrateHelper))]
namespace Reminders.Droid
{
    class VibrateHelper: IVibrate
    {
        void IVibrate.Vibrate()
        {
            Vibrate(Application.Context);
        }

        CanVibrateState IVibrate.CanVibrate()
        {
            return CanVibrate(Application.Context);
        }

        internal static void Vibrate(Context applicationContext)
        {
            var settings = ((App)App.Current).Settings;
            var milliseconds = Convert.ToInt32(Math.Round(settings.VibrateLength));
            var amplitude = Convert.ToInt32(Math.Round(settings.VibrateAmplitude));
            var v = (Vibrator)applicationContext.GetSystemService(Context.VibratorService);
            v.Vibrate(VibrationEffect.CreateOneShot(milliseconds, amplitude));
        }

        internal static CanVibrateState CanVibrate(Context applicationContext)
        {
            var settings = ((App)App.Current)?.Settings;
            if (settings.MinutesInterval <= 0)
            {
                return CanVibrateState.InvalidInterval;
            }
            if (!settings.On)
            {
                return CanVibrateState.Stopped;
            }
            if (settings.IgnoreIfNightMode && (applicationContext.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes)))
            {
                return CanVibrateState.Nightmode;
            }
            if (settings.IgnoreIfBetweenTimes)
            {
                var currentTime = settings.NextAlarm.TimeOfDay;
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