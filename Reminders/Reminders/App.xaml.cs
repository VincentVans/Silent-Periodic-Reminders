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
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            Settings.SaveState(App.Current.Properties);
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
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
        CanVibrateState CanVibrate();
    }

    public enum CanVibrateState
    {
        Yes,
        InvalidInterval,
        Stopped,
        Nightmode,
        Betweentimes,
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        bool on;
        int minutesInterval;
        double vibrateLength;
        bool ignoreIfNightMode;
        bool ignoreIfBetweenTimes;
        TimeSpan ignoreTimeStart;
        TimeSpan ignoreTimeEnd;
        internal DateTime mostRecentAlarmAttempt;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel(IDictionary<string, object> dictionary)
        {
            On = GetDictionaryEntry(dictionary, nameof(On), false);
            MinutesInterval = GetDictionaryEntry(dictionary, nameof(MinutesInterval), 15);
            VibrateLength = GetDictionaryEntry(dictionary, nameof(VibrateLength), 700.0);
            IgnoreIfNightMode = GetDictionaryEntry(dictionary, nameof(IgnoreIfNightMode), true);
            IgnoreIfBetweenTimes = GetDictionaryEntry(dictionary, nameof(IgnoreIfBetweenTimes), false);
            IgnoreTimeStart = GetDictionaryEntry(dictionary, nameof(IgnoreTimeStart), new TimeSpan(22, 0, 0));
            IgnoreTimeEnd = GetDictionaryEntry(dictionary, nameof(IgnoreTimeEnd), new TimeSpan(8, 0, 0));
            MostRecentAlarmAttempt = GetDictionaryEntry(dictionary, nameof(MostRecentAlarmAttempt), DateTime.UtcNow);
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

        public bool IgnoreIfNightMode
        {
            set { SetProperty(ref ignoreIfNightMode, value); }
            get { return ignoreIfNightMode; }
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
            get { return MostRecentAlarmAttempt.ToLocalTime().AddMinutes(MinutesInterval); }
        }

        public void SaveState(IDictionary<string, object> dictionary)
        {
            dictionary[nameof(On)] = On;
            dictionary[nameof(MinutesInterval)] = MinutesInterval;
            dictionary[nameof(VibrateLength)] = VibrateLength;
            dictionary[nameof(IgnoreIfNightMode)] = IgnoreIfNightMode;
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class EntryToMinutesConverter : IValueConverter
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
}
