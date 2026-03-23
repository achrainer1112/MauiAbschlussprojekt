using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly SleepApiService _sleepApiService;
        private readonly IReminderService _reminderService;

        private bool _isCalculating = false;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string weightInput = string.Empty;

        public string WeightKg
        {
            get => weightInput;
            set => WeightInput = value;
        }

        [ObservableProperty]
        private string customGoalInput = string.Empty;

        [ObservableProperty]
        private ActivityLevelOption? selectedActivityLevel;

        [ObservableProperty]
        private bool showCalculatedGoal;

        [ObservableProperty]
        private string calculatedGoalText = string.Empty;

        [ObservableProperty]
        private bool reminderEnabled;

        [ObservableProperty]
        private string reminderIntervalInput = string.Empty;

        [ObservableProperty]
        private int reminderStartHour = 8;

        [ObservableProperty]
        private int reminderEndHour = 22;

        [ObservableProperty]
        private string targetSleepHoursInput = "8";

        private TimeSpan _targetBedTime = new TimeSpan(23, 0, 0);
        public TimeSpan TargetBedTime
        {
            get => _targetBedTime;
            set
            {
                if (SetProperty(ref _targetBedTime, value))
                {
                    TargetBedTimeHour = value.Hours;
                    TargetBedTimeMinute = value.Minutes;
                    RecalculateWakeTime();
                }
            }
        }

        private TimeSpan _targetWakeTime = new TimeSpan(7, 0, 0);
        public TimeSpan TargetWakeTime
        {
            get => _targetWakeTime;
            set
            {
                if (SetProperty(ref _targetWakeTime, value))
                {
                    TargetWakeTimeHour = value.Hours;
                    TargetWakeTimeMinute = value.Minutes;
                    RecalculateSleepDuration();
                }
            }
        }

        private int TargetBedTimeHour = 23;
        private int TargetBedTimeMinute = 0;
        private int TargetWakeTimeHour = 7;
        private int TargetWakeTimeMinute = 0;

        [ObservableProperty]
        private bool sleepReminderEnabled;

        [ObservableProperty]
        private bool isLoading;


        public List<ActivityLevelOption> ActivityLevels { get; } = new()
        {
            new ActivityLevelOption { Value = "low",    Display = "Niedrig (30ml/kg)" },
            new ActivityLevelOption { Value = "medium", Display = "Mittel (35ml/kg)"  },
            new ActivityLevelOption { Value = "high",   Display = "Hoch (40ml/kg)"    }
        };
        public List<ActivityLevelOption> ActivityLevelOptions => ActivityLevels;

        public SettingsViewModel(ApiService apiService, SleepApiService sleepApiService, IReminderService reminderService)
        {
            _apiService = apiService;
            _sleepApiService = sleepApiService;
            _reminderService = reminderService;
        }

        partial void OnTargetSleepHoursInputChanged(string value) => RecalculateWakeTime();

        private void RecalculateWakeTime()
        {
            if (_isCalculating) return;
            if (!double.TryParse(TargetSleepHoursInput, out double hours) || hours <= 0) return;
            _isCalculating = true;
            TargetWakeTime = TargetBedTime.Add(TimeSpan.FromHours(hours));
            _isCalculating = false;
        }

        private void RecalculateSleepDuration()
        {
            if (_isCalculating) return;
            _isCalculating = true;
            double hours = (TargetWakeTime - TargetBedTime).TotalHours;
            if (hours < 0) hours += 24;
            TargetSleepHoursInput = Math.Round(hours, 1).ToString("F1");
            _isCalculating = false;
        }

        public async Task InitializeAsync()
        {
            await LoadCurrentSettingsAsync();
        }

        private async Task LoadCurrentSettingsAsync()
        {
            try
            {
                var user = await _apiService.GetUserAsync();

                if (user != null)
                {
                    Username = user.Username;
                    WeightInput = user.WeightKg?.ToString() ?? string.Empty;
                    CustomGoalInput = user.DailyWaterGoalMl?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(user.ActivityLevel))
                        SelectedActivityLevel = ActivityLevels.FirstOrDefault(a => a.Value == user.ActivityLevel);

                    ReminderEnabled = user.ReminderEnabled;
                    ReminderIntervalInput = user.ReminderIntervalMinutes.ToString();
                    ReminderStartHour = user.ReminderStartHour;
                    ReminderEndHour = user.ReminderEndHour;
                    SleepReminderEnabled = user.SleepReminderEnabled;

                    _isCalculating = true;
                    TargetBedTime = new TimeSpan(user.TargetBedTimeHour, user.TargetBedTimeMinute, 0);
                    TargetWakeTime = new TimeSpan(user.TargetWakeTimeHour, user.TargetWakeTimeMinute, 0);
                    TargetSleepHoursInput = user.TargetSleepHours.ToString("F1");
                    _isCalculating = false;

                    UpdateCalculatedGoal();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Einstellungen konnten nicht geladen werden: {ex.Message}", "OK");
            }
        }

        private void UpdateCalculatedGoal()
        {
            if (double.TryParse(WeightInput, out double weight) && SelectedActivityLevel != null)
            {
                double multiplier = SelectedActivityLevel.Value switch
                {
                    "low" => 30,
                    "medium" => 35,
                    "high" => 40,
                    _ => 33
                };
                CalculatedGoalText = $"Empfohlenes Tagesziel: {(int)(weight * multiplier)} ml";
                ShowCalculatedGoal = true;
            }
            else
            {
                ShowCalculatedGoal = false;
            }
        }

        partial void OnWeightInputChanged(string value) => UpdateCalculatedGoal();
        partial void OnSelectedActivityLevelChanged(ActivityLevelOption? value) => UpdateCalculatedGoal();


        [RelayCommand]
        private async Task SaveProfile()
        {
            IsLoading = true;
            try
            {
                var request = new UpdateUserRequest
                {
                    Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
                    WeightKg = double.TryParse(WeightInput, out double w) ? w : null,
                    ActivityLevel = SelectedActivityLevel?.Value,
                    DailyWaterGoalMl = int.TryParse(CustomGoalInput, out int g) && g > 0 ? g : null
                };

                var result = await _apiService.UpdateUserAsync(request);

                if (result != null)
                {
                    if (Shell.Current is AppShell appShell)
                        appShell.UpdateFlyoutHeader();

                    await Shell.Current.DisplayAlert("Erfolg", "Profil gespeichert ✓", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Fehler", "Speichern fehlgeschlagen. Bitte erneut versuchen.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private async Task SaveReminderSettings()
        {
            IsLoading = true;
            try
            {
                var request = new UpdateReminderRequest
                {
                    ReminderEnabled = ReminderEnabled,
                    ReminderIntervalMinutes = int.TryParse(ReminderIntervalInput, out int interval) ? interval : null,
                    ReminderStartHour = ReminderStartHour,
                    ReminderEndHour = ReminderEndHour
                };

                var result = await _apiService.UpdateReminderSettingsAsync(request);

                if (ReminderEnabled)
                    _reminderService.StartPeriodicNotifications(interval, ReminderStartHour, ReminderEndHour);
                else
                    _reminderService.StopPeriodicNotifications();

                if (result != null)
                    await Shell.Current.DisplayAlert("Erfolg", "Wasser-Einstellungen gespeichert ✓", "OK");
                else
                    await Shell.Current.DisplayAlert("Fehler", "Speichern fehlgeschlagen.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private async Task SaveSleepSettings()
        {
            IsLoading = true;
            try
            {
                var request = new UpdateSleepSettingsRequest
                {
                    TargetBedTimeHour = TargetBedTimeHour,
                    TargetBedTimeMinute = TargetBedTimeMinute,
                    TargetWakeTimeHour = TargetWakeTimeHour,
                    TargetWakeTimeMinute = TargetWakeTimeMinute,
                    SleepReminderEnabled = SleepReminderEnabled
                };

                if (double.TryParse(TargetSleepHoursInput, out double targetHours))
                    request.TargetSleepHours = targetHours;

                var result = await _sleepApiService.UpdateSleepSettingsAsync(request);

                if (result != null)
                    await Shell.Current.DisplayAlert("Erfolg", "Schlaf-Einstellungen gespeichert ✓", "OK");
                else
                    await Shell.Current.DisplayAlert("Fehler", "Speichern fehlgeschlagen.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Abmelden", "Möchtest du dich wirklich abmelden?", "Ja", "Nein");

            if (!confirm) return;

            _apiService.Logout();
            Application.Current!.MainPage = new AppShell();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public class ActivityLevelOption
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }
}
