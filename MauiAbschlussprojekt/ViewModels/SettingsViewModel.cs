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

        [ObservableProperty]
        private string weightInput = string.Empty;

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

        [ObservableProperty]
        private int targetBedTimeHour = 23;

        [ObservableProperty]
        private int targetBedTimeMinute = 0;

        [ObservableProperty]
        private int targetWakeTimeHour = 7;

        [ObservableProperty]
        private int targetWakeTimeMinute = 0;

        [ObservableProperty]
        private string weekendTargetHoursInput = string.Empty;

        [ObservableProperty]
        private bool sleepReminderEnabled;

        [ObservableProperty]
        private bool isLoading;

        public List<ActivityLevelOption> ActivityLevels { get; } = new()
        {
            new ActivityLevelOption { Value = "low", Display = "Niedrig (30ml/kg)" },
            new ActivityLevelOption { Value = "medium", Display = "Mittel (35ml/kg)" },
            new ActivityLevelOption { Value = "high", Display = "Hoch (40ml/kg)" }
        };

        public SettingsViewModel(ApiService apiService, SleepApiService sleepApiService, IReminderService reminderService)
        {
            _apiService = apiService;
            _sleepApiService = sleepApiService;
            _reminderService = reminderService;
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
                    WeightInput = user.WeightKg?.ToString() ?? string.Empty;
                    CustomGoalInput = user.DailyWaterGoalMl?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(user.ActivityLevel))
                    {
                        SelectedActivityLevel = ActivityLevels.FirstOrDefault(a => a.Value == user.ActivityLevel);
                    }

                    ReminderEnabled = user.ReminderEnabled;
                    ReminderIntervalInput = user.ReminderIntervalMinutes.ToString();
                    ReminderStartHour = user.ReminderStartHour;
                    ReminderEndHour = user.ReminderEndHour;

                    TargetSleepHoursInput = user.TargetSleepHours.ToString("F1");
                    TargetBedTimeHour = user.TargetBedTimeHour;
                    TargetBedTimeMinute = user.TargetBedTimeMinute;
                    TargetWakeTimeHour = user.TargetWakeTimeHour;
                    TargetWakeTimeMinute = user.TargetWakeTimeMinute;
                    WeekendTargetHoursInput = user.WeekendTargetSleepHours?.ToString("F1") ?? string.Empty;
                    SleepReminderEnabled = user.SleepReminderEnabled;

                    UpdateCalculatedGoal();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Einstellungen konnten nicht geladen werden: {ex.Message}", "OK");
            }
        }

        partial void OnWeightInputChanged(string value)
        {
            UpdateCalculatedGoal();
        }

        partial void OnSelectedActivityLevelChanged(ActivityLevelOption? value)
        {
            UpdateCalculatedGoal();
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

                int calculatedGoal = (int)(weight * multiplier);
                CalculatedGoalText = $"Empfohlenes Tagesziel: {calculatedGoal} ml";
                ShowCalculatedGoal = true;
            }
            else
            {
                ShowCalculatedGoal = false;
            }
        }

        [RelayCommand]
        private async Task SaveUserSettingsAsync()
        {
            IsLoading = true;

            try
            {
                var request = new UpdateUserRequest();

                if (double.TryParse(WeightInput, out double weight))
                    request.WeightKg = weight;

                if (SelectedActivityLevel != null)
                    request.ActivityLevel = SelectedActivityLevel.Value;

                if (int.TryParse(CustomGoalInput, out int customGoal) && customGoal > 0)
                    request.DailyWaterGoalMl = customGoal;

                var result = await _apiService.UpdateUserAsync(request);

                if (result != null)
                {
                    await Shell.Current.DisplayAlert("Erfolg", "Einstellungen gespeichert", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveReminderSettingsAsync()
        {
            IsLoading = true;

            try
            {
                var request = new UpdateReminderRequest
                {
                    ReminderEnabled = ReminderEnabled
                };

                if (int.TryParse(ReminderIntervalInput, out int interval))
                    request.ReminderIntervalMinutes = interval;

                request.ReminderStartHour = ReminderStartHour;
                request.ReminderEndHour = ReminderEndHour;

                var result = await _apiService.UpdateReminderSettingsAsync(request);

                if (result != null)
                {
                    if (ReminderEnabled)
                    {
                        await _reminderService.RequestPermissionAsync();
                        _reminderService.StartPeriodicNotifications(interval, ReminderStartHour, ReminderEndHour);
                    }
                    else
                    {
                        _reminderService.StopPeriodicNotifications();
                    }

                    await Shell.Current.DisplayAlert("Erfolg", "Erinnerungen gespeichert", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveSleepSettingsAsync()
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

                if (!string.IsNullOrEmpty(WeekendTargetHoursInput) && double.TryParse(WeekendTargetHoursInput, out double weekendHours))
                    request.WeekendTargetSleepHours = weekendHours;

                var result = await _sleepApiService.UpdateSleepSettingsAsync(request);

                if (result != null)
                {
                    await Shell.Current.DisplayAlert("Erfolg", "Schlaf-Einstellungen gespeichert", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Abmelden",
                "Möchtest du dich wirklich abmelden?",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            _apiService.Logout();

            // Navigate to Login - muss absolute Route sein
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