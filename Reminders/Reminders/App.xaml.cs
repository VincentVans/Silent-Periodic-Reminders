using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Reminders
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Settings = new SettingsViewModel(Properties);
            var mainPage = new MainPage();
            MainPage = mainPage;
            Settings.PropertyChanged += mainPage.Settings_PropertyChanged;
        }

        protected override void OnStart()
        {
            ((MainPage)MainPage).SyncToSettings();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            Settings.SaveState(App.Current.Properties);
        }

        protected override void OnResume()
        {
            ((MainPage)MainPage).SyncToSettings();
        }

        public SettingsViewModel Settings { private set; get; }
    }

    public interface ISetAlarm
    {
        void SetAlarm();
        void CancelAlarm();
        string NextAlarm();
    }

    public interface IVibrate
    {
        void Vibrate();
    }

    public enum CanVibrateState
    {
        Yes,
        InvalidInterval,
        Stopped,
        Betweentimes,
    }

    public sealed class SettingsViewModel : INotifyPropertyChanged
    {
        bool on;
        int minutesInterval;
        double vibrateLength;
        bool ignoreIfBetweenTimes;
        TimeSpan ignoreTimeStart;
        TimeSpan ignoreTimeEnd;
        DateTime mostRecentAlarmAttempt;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel(IDictionary<string, object> dictionary)
        {
            On = GetDictionaryEntry(dictionary, nameof(On), false);
            MinutesInterval = GetDictionaryEntry(dictionary, nameof(MinutesInterval), 15);
            VibrateLength = GetDictionaryEntry(dictionary, nameof(VibrateLength), 700.0);
            IgnoreIfBetweenTimes = GetDictionaryEntry(dictionary, nameof(IgnoreIfBetweenTimes), false);
            IgnoreTimeStart = GetDictionaryEntry(dictionary, nameof(IgnoreTimeStart), new TimeSpan(22, 0, 0));
            IgnoreTimeEnd = GetDictionaryEntry(dictionary, nameof(IgnoreTimeEnd), new TimeSpan(8, 0, 0));
            MostRecentAlarmAttempt = GetDictionaryEntry(dictionary, nameof(MostRecentAlarmAttempt), DateTime.UtcNow);
        }

        internal string ToSerializeString()
        {
            return string.Join(";",
                on ? "1" : "0",
                minutesInterval.ToString(CultureInfo.InvariantCulture),
                (Convert.ToInt32(Math.Round(vibrateLength))).ToString(CultureInfo.InvariantCulture),
                ignoreIfBetweenTimes ? "1" : "0",
                ignoreTimeStart.Ticks.ToString(CultureInfo.InvariantCulture),
                ignoreTimeEnd.Ticks.ToString(CultureInfo.InvariantCulture),
                mostRecentAlarmAttempt.Ticks.ToString(CultureInfo.InvariantCulture)
                );
        }

        public bool On
        {
            set { SetProperty(ref on, value); }
            get { return on; }
        }

        public int MinutesInterval
        {
            set { SetProperty(ref minutesInterval, value); }
            get { return minutesInterval; }
        }

        public double VibrateLength
        {
            set { SetProperty(ref vibrateLength, value); }
            get { return vibrateLength; }
        }

        public TimeSpan IgnoreTimeStart
        {
            set { SetProperty(ref ignoreTimeStart, value); }
            get { return ignoreTimeStart; }
        }

        public TimeSpan IgnoreTimeEnd
        {
            set { SetProperty(ref ignoreTimeEnd, value); }
            get { return ignoreTimeEnd; }
        }

        public bool IgnoreIfBetweenTimes
        {
            set { SetProperty(ref ignoreIfBetweenTimes, value); }
            get { return ignoreIfBetweenTimes; }
        }

        public DateTime MostRecentAlarmAttempt
        {
            set { SetProperty(ref mostRecentAlarmAttempt, value); }
            get { return mostRecentAlarmAttempt; }
        }

        public DateTime NextAlarm
        {
            get
            {
                return Settings.NextAlarm(MostRecentAlarmAttempt, MinutesInterval);
            }
        }

        public void SaveState(IDictionary<string, object> dictionary)
        {
            dictionary[nameof(On)] = On;
            dictionary[nameof(MinutesInterval)] = MinutesInterval;
            dictionary[nameof(VibrateLength)] = VibrateLength;
            dictionary[nameof(IgnoreIfBetweenTimes)] = IgnoreIfBetweenTimes;
            dictionary[nameof(IgnoreTimeStart)] = IgnoreTimeStart;
            dictionary[nameof(IgnoreTimeEnd)] = IgnoreTimeEnd;
            dictionary[nameof(MostRecentAlarmAttempt)] = MostRecentAlarmAttempt;
        }

        T GetDictionaryEntry<T>(IDictionary<string, object> dictionary, string key, T defaultValue = default(T))
        {
            return dictionary.ContainsKey(key) ? (T)dictionary[key] : defaultValue;
        }

        bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Object.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal sealed class EntryToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.TryParse((string)value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out int integer) ? integer : -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int)value).ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class Settings
    {
        internal readonly bool On;
        internal readonly int MinutesInterval;
        internal readonly int VibrateLength;
        internal readonly bool IgnoreIfBetweenTimes;
        internal readonly TimeSpan IgnoreTimeStart;
        internal readonly TimeSpan IgnoreTimeEnd;
        internal readonly DateTime MostRecentAlarmAttempt;

        internal static DateTime NextAlarm(DateTime mostRecentAlarmAttempt, int minutesInterval)
        {
            var currentTime = DateTime.Now.ToLocalTime();
            if (mostRecentAlarmAttempt < currentTime && minutesInterval > 0)
            {
                while (mostRecentAlarmAttempt < currentTime)
                {
                    mostRecentAlarmAttempt = mostRecentAlarmAttempt.AddMinutes(minutesInterval);
                }
            }
            return mostRecentAlarmAttempt;
        }

        internal static Settings FromSerializeString(string str)
        {
            var split = str.Split(';');
            return new Settings(parseBool(split[0]),
                int.Parse(split[1]),
                int.Parse(split[2]),
                parseBool(split[3]),
                TimeSpan.FromTicks(long.Parse(split[4])),
                TimeSpan.FromTicks(long.Parse(split[5])),
                new DateTime(long.Parse(split[6])));
        }

        private static bool parseBool(string str)
        {
            return str == "1";
        }

        private Settings(
            bool on,
            int minutesInterval,
            int vibrateLength,
            bool ignoreIfBetweenTimes,
            TimeSpan ignoreTimeStart,
            TimeSpan ignoreTimeEnd,
            DateTime mostRecentAlarmAttempt)
        {
            On = on;
            MinutesInterval = minutesInterval;
            VibrateLength = vibrateLength;
            IgnoreIfBetweenTimes = ignoreIfBetweenTimes;
            IgnoreTimeStart = ignoreTimeStart;
            IgnoreTimeEnd = ignoreTimeEnd;
            MostRecentAlarmAttempt = mostRecentAlarmAttempt;
        }
    }
}