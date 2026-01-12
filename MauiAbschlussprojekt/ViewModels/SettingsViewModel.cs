using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string weightInput = string.Empty;

        [ObservableProperty]
        private ActivityLevelItem selectedActivityLevel;

        [ObservableProperty]
        private string customGoalInput = string.Empty;

        [ObservableProperty]
        private string calculatedGoalText = string.Empty;

        [ObservableProperty]
        private bool showCalculatedGoal;

        [ObservableProperty]
        private bool reminderEnabled;

        [ObservableProperty]
        private int selectedReminderInterval;

        [ObservableProperty]
        private int reminderStartHour = 8;

        [ObservableProperty]
        private int reminderEndHour = 22;

        [ObservableProperty]
        private bool isLoading;

        public ObservableCollection<ActivityLevelItem> ActivityLevels { get; }
        public ObservableCollection<int> ReminderIntervals { get; }

        public SettingsViewModel(ApiService apiService, INotificationService notificationService)
        {
            _apiService = apiService;
            _notificationService = notificationService;

            ActivityLevels = new ObservableCollection<ActivityLevelItem>
            {
                new ActivityLevelItem { Value = "low", Display = "Niedrig" },
                new ActivityLevelItem { Value = "medium", Display = "Mittel" },
                new ActivityLevelItem { Value = "high", Display = "Hoch" }
            };

            ReminderIntervals = new ObservableCollection<int> { 30, 60, 90, 120, 180 };

            selectedActivityLevel = ActivityLevels[1]; // Default: medium
            selectedReminderInterval = 60;
        }

        public async Task InitializeAsync()
        {
            var user = _apiService.CurrentUser;
            if (user != null)
            {
                WeightInput = user.WeightKg?.ToString() ?? string.Empty;
                CustomGoalInput = user.DailyWaterGoalMl?.ToString() ?? string.Empty;
                ReminderEnabled = user.ReminderEnabled;
                SelectedReminderInterval = user.ReminderIntervalMinutes;
                ReminderStartHour = user.ReminderStartHour;
                ReminderEndHour = user.ReminderEndHour;

                if (!string.IsNullOrEmpty(user.ActivityLevel))
                {
                    var level = ActivityLevels.FirstOrDefault(a => a.Value == user.ActivityLevel);
                    if (level != null)
                        SelectedActivityLevel = level;
                }

                CalculateWaterGoal();

                // Starte Notifications falls aktiviert
                if (ReminderEnabled)
                {
                    _notificationService.StartPeriodicNotifications(
                        SelectedReminderInterval,
                        ReminderStartHour,
                        ReminderEndHour
                    );
                }
            }
        }

        partial void OnWeightInputChanged(string value)
        {
            CalculateWaterGoal();
        }

        partial void OnSelectedActivityLevelChanged(ActivityLevelItem value)
        {
            CalculateWaterGoal();
        }

        private void CalculateWaterGoal()
        {
            if (double.TryParse(WeightInput, out double weight) && weight > 0)
            {
                double multiplier = SelectedActivityLevel.Value switch
                {
                    "low" => 30,
                    "high" => 40,
                    _ => 35
                };
                int goal = (int)(weight * multiplier);
                CalculatedGoalText = $"Empfohlung: {goal} ml";
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

                if (double.TryParse(WeightInput, out double weight) && weight > 0)
                {
                    request.WeightKg = weight;
                    request.ActivityLevel = SelectedActivityLevel.Value;
                }

                if (int.TryParse(CustomGoalInput, out int customGoal) && customGoal > 0)
                {
                    request.DailyWaterGoalMl = customGoal;
                }

                var result = await _apiService.UpdateUserAsync(request);

                if (result != null)
                {
                    await Shell.Current.DisplayAlert("Erfolg", "Einstellungen gespeichert", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Fehler", "Einstellungen konnten nicht gespeichert werden", "OK");
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
        private async Task SaveReminderSettingsAsync()
        {
            IsLoading = true;

            try
            {
                // Berechtigung prüfen falls noch nicht vorhanden
                if (ReminderEnabled)
                {
                    bool hasPermission = await _notificationService.RequestPermissionAsync();
                    if (!hasPermission)
                    {
                        await Shell.Current.DisplayAlert("Berechtigung fehlt",
                            "Bitte erlaube Benachrichtigungen in den App-Einstellungen", "OK");
                        ReminderEnabled = false;
                        IsLoading = false;
                        return;
                    }
                }

                var request = new UpdateReminderRequest
                {
                    ReminderEnabled = ReminderEnabled,
                    ReminderIntervalMinutes = SelectedReminderInterval,
                    ReminderStartHour = ReminderStartHour,
                    ReminderEndHour = ReminderEndHour
                };

                var result = await _apiService.UpdateReminderSettingsAsync(request);

                if (result != null)
                {
                    // Notifications starten/stoppen
                    if (ReminderEnabled)
                    {
                        _notificationService.StartPeriodicNotifications(
                            SelectedReminderInterval,
                            ReminderStartHour,
                            ReminderEndHour
                        );
                    }
                    else
                    {
                        _notificationService.StopPeriodicNotifications();
                    }

                    await Shell.Current.DisplayAlert("Erfolg", "Erinnerungen gespeichert", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Fehler", "Erinnerungen konnten nicht gespeichert werden", "OK");
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
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}