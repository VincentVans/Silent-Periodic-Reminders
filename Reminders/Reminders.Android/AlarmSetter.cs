using Android.App;
using Android.Content;
using Android.OS;
using Reminders.Droid;
using System;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmSetter))]
namespace Reminders.Droid
{
    public sealed class AlarmSetter : ISetAlarm
    {
        internal const int alarmRequestCode = 0;
        internal const string settingsKey = "settings";

        public void SetAlarm()
        {
            ((App)App.Current).Settings.MostRecentAlarmAttempt = SetAlarm(Application.Context, ((App)App.Current).Settings.ToSerializeString(), ((App)App.Current).Settings.MinutesInterval);
        } 

        //TODO Switch to localBroadcastManager if you can get it working
        public static DateTime SetAlarm(Context context, string serializedSettings, int minutesInterval)
        {
            if (minutesInterval >= Settings.minimumInterval)
            {
                var am = (AlarmManager)context.GetSystemService(Context.AlarmService);
                var pIntent = PendingIntent.GetBroadcast(context, alarmRequestCode, ReminderIntent(context, serializedSettings), PendingIntentFlags.UpdateCurrent);
                var intervalMillis = SystemClock.ElapsedRealtime() + (minutesInterval * 60000L);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    am.SetExactAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, intervalMillis, pIntent);
                }
                else
                {
                    am.SetExact(AlarmType.ElapsedRealtimeWakeup, intervalMillis, pIntent);
                }
            }
            return DateTime.Now;
        }

        public void CancelAlarm()
        {
            var am = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            var pIntent = PendingIntent.GetBroadcast(Application.Context, alarmRequestCode, ReminderIntent(Application.Context, ((App)App.Current).Settings.ToSerializeString()), PendingIntentFlags.CancelCurrent);
            am.Cancel(pIntent);
            pIntent.Cancel();
        }

        public string NextAlarm()
        {
            //TODO Kinda lame to serialize and deserialize, but whatever.
            switch (VibrateHelper.CanVibrate(Application.Context, Settings.FromSerializeString(((App)App.Current).Settings.ToSerializeString())))
            {
                case CanVibrateState.InvalidInterval:
                    return "N/A: Invalid 'minutes between reminders': must be a whole number, at least " + Settings.minimumInterval;
                case CanVibrateState.Stopped:
                    return "N/A: Haven't started yet";
                case CanVibrateState.Betweentimes:
                    return "N/A: Within excluded period";
                default:
                    return ((App)App.Current).Settings.NextAlarm.ToShortTimeString();
            }
        }

        private static Intent ReminderIntent(Context applicationContext, string serializedSettings)
        {
            var intent = new Intent(applicationContext, typeof(ReminderBroadcastReceiver));
            intent.PutExtra(settingsKey, serializedSettings);
            return intent;
        }
    }

    [BroadcastReceiver(Enabled = true, Process = ":remote")]
    public sealed class ReminderBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var serialized = intent.GetStringExtra(AlarmSetter.settingsKey);
            var s = Settings.FromSerializeString(serialized);
            AlarmSetter.SetAlarm(context, serialized, s.MinutesInterval);
            if (VibrateHelper.CanVibrate(context, s) == CanVibrateState.Yes)
            {
                VibrateHelper.Vibrate(context, s.VibrateLength);
            }
            context.SendBroadcast(UpdateReminderIntent(context));
        }

        private static Intent UpdateReminderIntent(Context applicationContext)
        {
            return new Intent(applicationContext, typeof(UpdateReminderLabelBroadcastReceiver));
        }
    }

    [BroadcastReceiver(Enabled = true)]
    public sealed class UpdateReminderLabelBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ((App)App.Current).Settings.MostRecentAlarmAttempt = DateTime.Now;
        }
    }
}