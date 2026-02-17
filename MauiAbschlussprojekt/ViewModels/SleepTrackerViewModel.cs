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

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private double targetSleepHours = 8.0;
        [ObservableProperty] private double lastNightSleepHours;
        [ObservableProperty] private int lastNightQuality;
        [ObservableProperty] private string lastNightSummary = "Noch keine Daten";
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool hasLastNightData;

        // QuickBedTime bleibt erhalten solange das ViewModel (Singleton) lebt
        private DateTime? _quickBedTime;
        public string QuickBedTimeDisplay => _quickBedTime.HasValue
            ? $"🛏️ Ins Bett: {_quickBedTime:HH:mm} Uhr"
            : "🛏️ Noch nicht geloggt";
        public bool HasQuickBedTime => _quickBedTime.HasValue;

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
                foreach (var e in entries) RecentEntries.Add(e);

                var last = entries.FirstOrDefault();
                if (last != null)
                {
                    HasLastNightData = true;
                    LastNightSleepHours = last.TotalSleepHours;
                    LastNightQuality = last.SleepQuality;
                    LastNightSummary = $"{last.TotalSleepHours:F1}h geschlafen • {GetQualityText(last.SleepQuality)}";
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
            finally { IsLoading = false; }
        }

        // ── Quick-Log: Ins Bett ───────────────────────────────────────────────
        // Speichert Zeitstempel, zeigt kurze Bestätigung – kein Popup das nervt
        [RelayCommand]
        private void QuickLogBedtime()
        {
            _quickBedTime = DateTime.Now;
            OnPropertyChanged(nameof(QuickBedTimeDisplay));
            OnPropertyChanged(nameof(HasQuickBedTime));
            // Kurzes visuelles Feedback – kein blockierender Dialog
        }

        // ── Quick-Log: Aufgewacht → direkt zur AddSleepEntryPage ─────────────
        [RelayCommand]
        private async Task QuickLogWakeupAsync()
        {
            var wakeTime = DateTime.Now;

            // Bett-Zeit: falls noch kein QuickBedTime geloggt → ca. 8h vor Aufwachen schätzen
            var bedTime = _quickBedTime ?? wakeTime.AddHours(-8);

            // Direkt navigieren – Zeiten werden als QueryProperties übergeben
            await Shell.Current.GoToAsync(
                $"AddSleepEntryPage?bedTime={bedTime:O}&wakeTime={wakeTime:O}");

            // QuickBedTime zurücksetzen nach dem Loggen
            _quickBedTime = null;
            OnPropertyChanged(nameof(QuickBedTimeDisplay));
            OnPropertyChanged(nameof(HasQuickBedTime));
        }

        // ── Manueller Eintrag ────────────────────────────────────────────────
        [RelayCommand]
        private async Task AddManualEntryAsync()
            => await Shell.Current.GoToAsync("AddSleepEntryPage");

        // ── Eintrag bearbeiten ───────────────────────────────────────────────
        [RelayCommand]
        private async Task ViewEntryDetailsAsync(SleepEntryDto entry)
            => await Shell.Current.GoToAsync($"AddSleepEntryPage?entryId={entry.Id}");

        // ── Eintrag löschen ──────────────────────────────────────────────────
        [RelayCommand]
        private async Task DeleteEntryAsync(SleepEntryDto entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Löschen",
                $"Eintrag vom {entry.BedTime.ToLocalTime():dd.MM.yyyy} wirklich löschen?",
                "Ja", "Nein");
            if (!confirm) return;

            try
            {
                if (await _sleepApiService.DeleteSleepEntryAsync(entry.Id))
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
            => await Shell.Current.GoToAsync("SleepStatsPage");

        private static string GetQualityText(int q) => q switch
        {
            5 => "Ausgezeichnet 😄",
            4 => "Gut 🙂",
            3 => "Okay 😐",
            2 => "Schlecht 😕",
            _ => "Sehr schlecht 😴"
        };
    }
}