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
        private bool bedTimeLogged;

        [ObservableProperty]
        private string bedTimeDisplay = string.Empty;

        [ObservableProperty]
        private double lastNightProgress;

        private DateTime _loggedBedTime;

        public ObservableCollection<SleepEntryDto> RecentEntries { get; } = new();

        public SleepTrackerViewModel(SleepApiService sleepApiService, ApiService apiService)
        {
            _sleepApiService = sleepApiService;
            _apiService = apiService;
        }

        public async Task InitializeAsync()
        {
            var freshUser = await _apiService.GetUserAsync();
            Username = freshUser?.Username ?? _apiService.CurrentUser?.Username ?? "User";
            TargetSleepHours = freshUser?.TargetSleepHours ?? _apiService.CurrentUser?.TargetSleepHours ?? 8.0;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var detailed = await _sleepApiService.GetDetailedStatsAsync(30);
                List<SleepEntryDto> entries;

                if (detailed != null && detailed.RecentEntries != null && detailed.RecentEntries.Any())
                    entries = detailed.RecentEntries;
                else
                    entries = await _sleepApiService.GetRecentEntriesAsync(7);

                entries = entries.OrderByDescending(e => e.BedTime).ToList();

                RecentEntries.Clear();
                foreach (var entry in entries)
                    RecentEntries.Add(entry);

                var lastNight = entries.FirstOrDefault();
                if (lastNight != null)
                {
                    HasLastNightData = true;
                    LastNightSleepHours = lastNight.TotalSleepHours;
                    LastNightQuality = lastNight.SleepQuality;
                    LastNightSummary = $"{lastNight.TotalSleepHours:F1}h geschlafen • {GetQualityText(lastNight.SleepQuality)}";

                    LastNightProgress = TargetSleepHours > 0
                        ? Math.Min(lastNight.TotalSleepHours / TargetSleepHours, 1.0)
                        : 0.0;
                }
                else
                {
                    HasLastNightData = false;
                    LastNightSummary = "Noch keine Daten";
                    LastNightProgress = 0.0;
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
        private void QuickLogBedtime()
        {
            _loggedBedTime = DateTime.Now;
            BedTimeLogged = true;
            BedTimeDisplay = _loggedBedTime.ToString("HH:mm") + " Uhr";
        }

        [RelayCommand]
        private async Task QuickLogWakeupAsync()
        {
            if (!BedTimeLogged)
            {
                await Shell.Current.DisplayAlert("Hinweis", "Bitte zuerst den 'Ins Bett' Button drücken.", "OK");
                return;
            }

            var wakeTime = DateTime.Now;
            var bedParam = _loggedBedTime.ToString("yyyyMMddHHmm");
            var wakeParam = wakeTime.ToString("yyyyMMddHHmm");

            await Shell.Current.GoToAsync($"AddSleepEntryPage?bedTime={bedParam}&wakeTime={wakeParam}");

            BedTimeLogged = false;
            BedTimeDisplay = string.Empty;
        }

        [RelayCommand]
        private async Task AddManualEntryAsync()
        {
            await Shell.Current.GoToAsync("AddSleepEntryPage");
        }

        [RelayCommand]
        private async Task NavigateToStatsAsync()
        {
            await Shell.Current.GoToAsync("SleepStatsPage");
        }

        /// <summary>
        /// Navigiert zur SleepStatsPage und scrollt dort zum Traumtagebuch-Bereich.
        /// Da wir keine direkte Scroll-Navigation haben, übergeben wir einen Parameter.
        /// </summary>
        [RelayCommand]
        private async Task NavigateToDreamsAsync()
        {
            await Shell.Current.GoToAsync("SleepStatsPage?showDreams=true");
        }

        [RelayCommand]
        private async Task DeleteEntryAsync(SleepEntryDto entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Löschen",
                $"Eintrag vom {entry.BedTime:dd.MM.yyyy} wirklich löschen?",
                "Ja, löschen", "Abbrechen");

            if (!confirm) return;

            try
            {
                bool success = await _sleepApiService.DeleteSleepEntryAsync(entry.Id);
                if (success)
                {
                    RecentEntries.Remove(entry);
                    // Letzte-Nacht-Anzeige aktualisieren
                    var newLast = RecentEntries.FirstOrDefault();
                    if (newLast != null)
                    {
                        HasLastNightData = true;
                        LastNightSleepHours = newLast.TotalSleepHours;
                        LastNightQuality = newLast.SleepQuality;
                        LastNightSummary = $"{newLast.TotalSleepHours:F1}h geschlafen • {GetQualityText(newLast.SleepQuality)}";
                        LastNightProgress = TargetSleepHours > 0
                            ? Math.Min(newLast.TotalSleepHours / TargetSleepHours, 1.0)
                            : 0.0;
                    }
                    else
                    {
                        HasLastNightData = false;
                        LastNightSummary = "Noch keine Daten";
                        LastNightProgress = 0.0;
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Fehler", "Löschen fehlgeschlagen.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Löschen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ViewEntryDetailsAsync(SleepEntryDto entry)
        {
            await Shell.Current.GoToAsync($"AddSleepEntryPage?entryId={entry.Id}");
        }

        private static string GetQualityText(int quality) => quality switch
        {
            5 => "Ausgezeichnet 😄",
            4 => "Gut 🙂",
            3 => "OK 😐",
            2 => "Schlecht 😕",
            1 => "Sehr schlecht 😴",
            _ => "Unbekannt"
        };
    }
}