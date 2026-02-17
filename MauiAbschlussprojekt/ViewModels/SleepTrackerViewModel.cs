using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class SleepTrackerViewModel : ObservableObject
    {
        private readonly SleepApiService _sleepApiService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private double targetSleepHours = 8.0;

        [ObservableProperty]
        private double lastNightSleepHours;

        [ObservableProperty]
        private int lastNightQuality;

        [ObservableProperty]
        private string lastNightSummary = "Noch keine Daten";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasLastNightData;

        [ObservableProperty]
        private DateTime quickBedTime = DateTime.Now;

        [ObservableProperty]
        private DateTime quickWakeTime = DateTime.Now;

        public ObservableCollection<SleepEntryDto> RecentEntries { get; } = new();

        public SleepTrackerViewModel(SleepApiService sleepApiService, ApiService apiService)
        {
            _sleepApiService = sleepApiService;
            _apiService = apiService;
        }

        public async Task InitializeAsync()
        {
            Username = _apiService.CurrentUser?.Username ?? "User";
            TargetSleepHours = _apiService.CurrentUser?.TargetSleepHours ?? 8.0;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var entries = await _sleepApiService.GetRecentEntriesAsync(7);

                RecentEntries.Clear();
                foreach (var entry in entries)
                {
                    RecentEntries.Add(entry);
                }

                var lastNight = entries.FirstOrDefault();
                if (lastNight != null)
                {
                    HasLastNightData = true;
                    LastNightSleepHours = lastNight.TotalSleepHours;
                    LastNightQuality = lastNight.SleepQuality;
                    LastNightSummary = $"{lastNight.TotalSleepHours:F1}h geschlafen • {GetQualityText(lastNight.SleepQuality)}";
                }
                else
                {
                    HasLastNightData = false;
                    LastNightSummary = "Noch keine Daten";
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Daten konnten nicht geladen werden: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task QuickLogBedtimeAsync()
        {
            QuickBedTime = DateTime.Now;
            await Shell.Current.DisplayAlert("Gute Nacht!", $"Ins Bett gegangen um {QuickBedTime:HH:mm} Uhr", "OK");
        }

        [RelayCommand]
        private async Task QuickLogWakeupAsync()
        {
            QuickWakeTime = DateTime.Now;

            bool confirm = await Shell.Current.DisplayAlert(
                "Aufgewacht",
                $"Aufgewacht um {QuickWakeTime:HH:mm} Uhr. Eintrag erstellen?",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            await Shell.Current.GoToAsync($"AddSleepEntryPage?bedTime={QuickBedTime:O}&wakeTime={QuickWakeTime:O}");
        }

        [RelayCommand]
        private async Task AddManualEntryAsync()
        {
            await Shell.Current.GoToAsync("AddSleepEntryPage");
        }

        [RelayCommand]
        private async Task ViewEntryDetailsAsync(SleepEntryDto entry)
        {
            await Shell.Current.GoToAsync($"AddSleepEntryPage?entryId={entry.Id}");
        }

        [RelayCommand]
        private async Task DeleteEntryAsync(SleepEntryDto entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Löschen",
                $"Eintrag vom {entry.BedTime:dd.MM.yyyy} wirklich löschen?",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            try
            {
                bool success = await _sleepApiService.DeleteSleepEntryAsync(entry.Id);

                if (success)
                {
                    RecentEntries.Remove(entry);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Löschen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateToStatsAsync()
        {
            await Shell.Current.GoToAsync("SleepStatsPage");
        }

        private string GetQualityText(int quality)
        {
            return quality switch
            {
                5 => "Ausgezeichnet",
                4 => "Gut",
                3 => "OK",
                2 => "Schlecht",
                1 => "Sehr schlecht",
                _ => "Unbekannt"
            };
        }
    }
}