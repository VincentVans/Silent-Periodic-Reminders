using Android.App;
using Android.Content;
using Reminders.Droid;
using System;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmSetter))]
namespace Reminders.Droid
{
    public class AlarmSetter : ISetAlarm
    {
        internal const int alarmRequestCode = 0;

        //TODO Switch to localBroadcastManager if you can get it working
        public void SetAlarm()
        {
            var am = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            var pIntent = PendingIntent.GetBroadcast(Application.Context, alarmRequestCode, ReminderIntent(Application.Context), PendingIntentFlags.UpdateCurrent);
            var intervalMillis = ((App)App.Current).Settings.MinutesInterval * 60000L;
            am.SetInexactRepeating(AlarmType.ElapsedRealtimeWakeup, intervalMillis, intervalMillis, pIntent);
            ((App)App.Current).Settings.MostRecentAlarmAttempt = DateTime.UtcNow;
        }

        public void CancelAlarm()
        {
            var am = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            var pIntent = PendingIntent.GetBroadcast(Application.Context, alarmRequestCode, ReminderIntent(Application.Context), PendingIntentFlags.CancelCurrent);
            am.Cancel(pIntent);
            pIntent.Cancel();
        }

        public string NextAlarm()
        {
            switch (VibrateHelper.CanVibrate(Application.Context))
            {
                case CanVibrateState.InvalidInterval:
                    return "N/A: Invalid 'minutes between reminders'";
                case CanVibrateState.Stopped:
                    return "N/A: Haven't started yet";
                case CanVibrateState.Nightmode:
                    return "N/A: Night mode is on";
                case CanVibrateState.Silentmode:
                    return "N/A: Silent mode is on";
                case CanVibrateState.Betweentimes:
                    return "N/A: Within excluded period";
                default:
                    return ((App)App.Current).Settings.NextAlarm.ToShortTimeString();
            }
        }

        private static Intent ReminderIntent(Context applicationContext)
        {
            return new Intent(applicationContext, typeof(ReminderBroadcastReceiver));
        }
    }

    [BroadcastReceiver(Enabled = true)]
    public class ReminderBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ((App)App.Current).Settings.MostRecentAlarmAttempt = DateTime.UtcNow;
            if (VibrateHelper.CanVibrate(context) == CanVibrateState.Yes)
            {
                VibrateHelper.Vibrate(context);
            }
        }
    }
}