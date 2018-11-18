using Android.App;
using Android.Content;
using Reminders.Droid;
using System;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmSetter))]
namespace Reminders.Droid
{
    public sealed class AlarmSetter : ISetAlarm
    {
        internal const int alarmRequestCode = 0;
        internal const string settingsKey = "settings";

        //TODO Switch to localBroadcastManager if you can get it working
        public void SetAlarm()
        {
            var am = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            var pIntent = PendingIntent.GetBroadcast(Application.Context, alarmRequestCode, ReminderIntent(Application.Context), PendingIntentFlags.UpdateCurrent);
            var intervalMillis = ((App)App.Current).Settings.MinutesInterval * 60000L;
            am.SetInexactRepeating(AlarmType.ElapsedRealtimeWakeup, intervalMillis, intervalMillis, pIntent);
            ((App)App.Current).Settings.MostRecentAlarmAttempt = DateTime.Now;
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
            //TODO Kinda lame to serialize and deserialize, but whatever.
            switch (VibrateHelper.CanVibrate(Application.Context, Settings.FromSerializeString(((App)App.Current).Settings.ToSerializeString())))
            {
                case CanVibrateState.InvalidInterval:
                    return "N/A: Invalid 'minutes between reminders'";
                case CanVibrateState.Stopped:
                    return "N/A: Haven't started yet";
                case CanVibrateState.Betweentimes:
                    return "N/A: Within excluded period";
                default:
                    return ((App)App.Current).Settings.NextAlarm.ToShortTimeString();
            }
        }

        private static Intent ReminderIntent(Context applicationContext)
        {
            var intent = new Intent(applicationContext, typeof(ReminderBroadcastReceiver));
            intent.PutExtra(settingsKey, ((App)App.Current).Settings.ToSerializeString());
            return intent;
        }
    }

    [BroadcastReceiver(Enabled = true, Process = ":remote")]
    public sealed class ReminderBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var s = Settings.FromSerializeString(intent.GetStringExtra(AlarmSetter.settingsKey));
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
            ((MainPage)App.Current.MainPage).SyncToSettings();
        }
    }
}