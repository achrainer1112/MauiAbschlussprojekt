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

        // ── Wasser ──────────────────────────────────────────────────────────
        [ObservableProperty] private string weightInput = string.Empty;
        [ObservableProperty] private string customGoalInput = string.Empty;
        [ObservableProperty] private ActivityLevelOption? selectedActivityLevel;
        [ObservableProperty] private bool showCalculatedGoal;
        [ObservableProperty] private string calculatedGoalText = string.Empty;

        // ── Wasser-Erinnerungen ──────────────────────────────────────────────
        [ObservableProperty] private bool reminderEnabled;
        [ObservableProperty] private string reminderIntervalInput = string.Empty;
        [ObservableProperty] private int reminderStartHour = 8;
        [ObservableProperty] private int reminderEndHour = 22;

        // ── Schlaf ───────────────────────────────────────────────────────────
        [ObservableProperty] private string targetSleepHoursInput = "8";
        [ObservableProperty] private int targetBedTimeHour = 23;
        [ObservableProperty] private int targetBedTimeMinute = 0;
        [ObservableProperty] private int targetWakeTimeHour = 7;
        [ObservableProperty] private int targetWakeTimeMinute = 0;
        [ObservableProperty] private bool sleepReminderEnabled;
        [ObservableProperty] private bool isLoading;

        // ── Computed: formatierte Anzeige ────────────────────────────────────
        // Stunden/Minuten als 2-stelliger String für die Picker-Labels
        public string TargetBedTimeHourDisplay => $"{TargetBedTimeHour % 24:D2}";
        public string TargetBedTimeMinuteDisplay => $"{TargetBedTimeMinute:D2}";
        public string TargetWakeTimeHourDisplay => $"{TargetWakeTimeHour:D2}";
        public string TargetWakeTimeMinuteDisplay => $"{TargetWakeTimeMinute:D2}";

        // Lesbarer Text unterhalb der Picker
        public string TargetBedTimeDisplay =>
            TargetBedTimeHour >= 24
                ? $"{TargetBedTimeHour % 24:D2}:{TargetBedTimeMinute:D2} Uhr (+1 Tag)"
                : $"{TargetBedTimeHour:D2}:{TargetBedTimeMinute:D2} Uhr";

        public string TargetWakeTimeDisplay =>
            $"{TargetWakeTimeHour:D2}:{TargetWakeTimeMinute:D2} Uhr";

        // ── Aktivitätslevel ──────────────────────────────────────────────────
        public List<ActivityLevelOption> ActivityLevels { get; } = new()
        {
            new ActivityLevelOption { Value = "low",    Display = "Niedrig (30ml/kg)" },
            new ActivityLevelOption { Value = "medium", Display = "Mittel (35ml/kg)"  },
            new ActivityLevelOption { Value = "high",   Display = "Hoch (40ml/kg)"    }
        };

        public SettingsViewModel(ApiService apiService, SleepApiService sleepApiService, IReminderService reminderService)
        {
            _apiService = apiService;
            _sleepApiService = sleepApiService;
            _reminderService = reminderService;
        }

        public async Task InitializeAsync() => await LoadCurrentSettingsAsync();

        // ── Daten laden ──────────────────────────────────────────────────────
        private async Task LoadCurrentSettingsAsync()
        {
            try
            {
                var user = await _apiService.GetUserAsync();
                if (user == null) return;

                WeightInput = user.WeightKg?.ToString() ?? string.Empty;
                CustomGoalInput = user.DailyWaterGoalMl?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(user.ActivityLevel))
                    SelectedActivityLevel = ActivityLevels.FirstOrDefault(a => a.Value == user.ActivityLevel);

                ReminderEnabled = user.ReminderEnabled;
                ReminderIntervalInput = user.ReminderIntervalMinutes.ToString();
                ReminderStartHour = user.ReminderStartHour;
                ReminderEndHour = user.ReminderEndHour;

                TargetSleepHoursInput = user.TargetSleepHours.ToString("F1");
                TargetBedTimeHour = user.TargetBedTimeHour;
                TargetBedTimeMinute = user.TargetBedTimeMinute;
                TargetWakeTimeHour = user.TargetWakeTimeHour;
                TargetWakeTimeMinute = user.TargetWakeTimeMinute;
                SleepReminderEnabled = user.SleepReminderEnabled;

                UpdateCalculatedGoal();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Einstellungen konnten nicht geladen werden: {ex.Message}", "OK");
            }
        }

        // ── Property-Changed Hooks ───────────────────────────────────────────
        partial void OnTargetBedTimeHourChanged(int value)
        {
            OnPropertyChanged(nameof(TargetBedTimeHourDisplay));
            OnPropertyChanged(nameof(TargetBedTimeDisplay));
        }
        partial void OnTargetBedTimeMinuteChanged(int value)
        {
            OnPropertyChanged(nameof(TargetBedTimeMinuteDisplay));
            OnPropertyChanged(nameof(TargetBedTimeDisplay));
        }
        partial void OnTargetWakeTimeHourChanged(int value)
        {
            OnPropertyChanged(nameof(TargetWakeTimeHourDisplay));
            OnPropertyChanged(nameof(TargetWakeTimeDisplay));
        }
        partial void OnTargetWakeTimeMinuteChanged(int value)
        {
            OnPropertyChanged(nameof(TargetWakeTimeMinuteDisplay));
            OnPropertyChanged(nameof(TargetWakeTimeDisplay));
        }
        partial void OnWeightInputChanged(string value) => UpdateCalculatedGoal();
        partial void OnSelectedActivityLevelChanged(ActivityLevelOption? value) => UpdateCalculatedGoal();

        // ── Schlafenszeit-Picker Commands ─────────────────────────────────────
        // Stunden: 0–27 (24=00:00 nächster Tag, bis 03:00)
        [RelayCommand] private void IncrementBedHour() { if (TargetBedTimeHour < 27) TargetBedTimeHour++; }
        [RelayCommand] private void DecrementBedHour() { if (TargetBedTimeHour > 0) TargetBedTimeHour--; }

        // Minuten: 0 / 15 / 30 / 45
        [RelayCommand]
        private void IncrementBedMinute()
            => TargetBedTimeMinute = TargetBedTimeMinute >= 45 ? 0 : TargetBedTimeMinute + 15;
        [RelayCommand]
        private void DecrementBedMinute()
            => TargetBedTimeMinute = TargetBedTimeMinute <= 0 ? 45 : TargetBedTimeMinute - 15;

        // ── Aufwachzeit-Picker Commands ───────────────────────────────────────
        [RelayCommand] private void IncrementWakeHour() { if (TargetWakeTimeHour < 23) TargetWakeTimeHour++; }
        [RelayCommand] private void DecrementWakeHour() { if (TargetWakeTimeHour > 0) TargetWakeTimeHour--; }

        [RelayCommand]
        private void IncrementWakeMinute()
            => TargetWakeTimeMinute = TargetWakeTimeMinute >= 45 ? 0 : TargetWakeTimeMinute + 15;
        [RelayCommand]
        private void DecrementWakeMinute()
            => TargetWakeTimeMinute = TargetWakeTimeMinute <= 0 ? 45 : TargetWakeTimeMinute - 15;

        // ── Wasserziel berechnen ─────────────────────────────────────────────
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

        // ── Commands: Speichern ──────────────────────────────────────────────
        [RelayCommand]
        private async Task SaveUserSettingsAsync()
        {
            IsLoading = true;
            try
            {
                var request = new UpdateUserRequest();
                if (double.TryParse(WeightInput, out double weight)) request.WeightKg = weight;
                if (SelectedActivityLevel != null) request.ActivityLevel = SelectedActivityLevel.Value;
                if (int.TryParse(CustomGoalInput, out int goal) && goal > 0) request.DailyWaterGoalMl = goal;

                if (await _apiService.UpdateUserAsync(request) != null)
                    await Shell.Current.DisplayAlert("Erfolg", "Einstellungen gespeichert", "OK");
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Fehler", ex.Message, "OK"); }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task SaveReminderSettingsAsync()
        {
            IsLoading = true;
            try
            {
                int interval = int.TryParse(ReminderIntervalInput, out int iv) ? iv : 60;
                var request = new UpdateReminderRequest
                {
                    ReminderEnabled = ReminderEnabled,
                    ReminderIntervalMinutes = interval,
                    ReminderStartHour = ReminderStartHour,
                    ReminderEndHour = ReminderEndHour
                };

                if (await _apiService.UpdateReminderSettingsAsync(request) != null)
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
            catch (Exception ex) { await Shell.Current.DisplayAlert("Fehler", ex.Message, "OK"); }
            finally { IsLoading = false; }
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
                if (double.TryParse(TargetSleepHoursInput, out double h)) request.TargetSleepHours = h;

                var result = await _sleepApiService.UpdateSleepSettingsAsync(request);
                if (result != null)
                {
                    if (SleepReminderEnabled)
                    {
                        await _reminderService.RequestPermissionAsync();
                        ScheduleSleepReminder();
                    }
                    else
                    {
                        _reminderService.CancelSleepReminder();
                    }

                    await Shell.Current.DisplayAlert("Erfolg", "Schlaf-Einstellungen gespeichert", "OK");
                }
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Fehler", ex.Message, "OK"); }
            finally { IsLoading = false; }
        }

        private void ScheduleSleepReminder()
        {
            // Delegiert an den ReminderService – dieser berechnet intern
            // die Erinnerungszeit als 1 Stunde vor der Schlafenszeit und
            // prüft minütlich ob die Zeit erreicht ist (nur 1x pro Tag).
            _reminderService.ScheduleDailySleepReminder(TargetBedTimeHour, TargetBedTimeMinute);
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (!await Shell.Current.DisplayAlert("Abmelden", "Möchtest du dich wirklich abmelden?", "Ja", "Nein"))
                return;

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