using CommunityToolkit.Mvvm.ComponentModel;
using Models;
using MauiAbschlussprojekt.Services;

namespace MauiAbschlussprojekt.ViewModels
{
    [QueryProperty(nameof(BedTimeString), "bedTime")]
    [QueryProperty(nameof(WakeTimeString), "wakeTime")]
    [QueryProperty(nameof(EntryId), "entryId")]
    public partial class AddSleepEntryViewModel : ObservableObject
    {
        private readonly SleepApiService _sleepApiService;

        // QueryProperties (werden vor OnAppearing gesetzt)
        [ObservableProperty] private string bedTimeString = string.Empty;
        [ObservableProperty] private string wakeTimeString = string.Empty;
        [ObservableProperty] private int? entryId;

        // Berechnete Startwerte für die View
        public DateTime BedTime { get; private set; } = DateTime.Today.AddHours(23);
        public DateTime WakeTime { get; private set; } = DateTime.Today.AddDays(1).AddHours(7);

        // Edit-Modus: Initialwerte für Code-Behind
        public bool IsEditMode { get; private set; }
        public int InitialSleepQuality { get; private set; } = 3;
        public int InitialFallAsleepIndex { get; private set; } = 1;
        public string InitialDreamText { get; private set; } = string.Empty;
        public int InitialDreamMoodIndex { get; private set; } = 1;
        public string InitialNotes { get; private set; } = string.Empty;

        [ObservableProperty] private bool isLoading;

        public AddSleepEntryViewModel(SleepApiService sleepApiService)
        {
            _sleepApiService = sleepApiService;
        }

        /// <summary>Wird von OnAppearing aufgerufen. Verarbeitet QueryProperties und lädt ggf. Edit-Daten.</summary>
        public async Task InitializeAsync()
        {
            // QuickLog-Zeiten aus URL-Parametern parsen
            if (!string.IsNullOrEmpty(BedTimeString) &&
                DateTime.TryParse(BedTimeString, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsedBed))
            {
                BedTime = parsedBed.ToLocalTime();
            }

            if (!string.IsNullOrEmpty(WakeTimeString) &&
                DateTime.TryParse(WakeTimeString, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsedWake))
            {
                WakeTime = parsedWake.ToLocalTime();
            }

            // Edit-Modus
            if (EntryId.HasValue)
            {
                IsEditMode = true;
                await LoadEntryAsync(EntryId.Value);
            }
        }

        private async Task LoadEntryAsync(int id)
        {
            try
            {
                var entries = await _sleepApiService.GetRecentEntriesAsync(30);
                var entry = entries.FirstOrDefault(e => e.Id == id);
                if (entry == null) return;

                BedTime = entry.BedTime.ToLocalTime();
                WakeTime = entry.WakeTime.ToLocalTime();

                InitialFallAsleepIndex = entry.FallAsleepDurationCategory switch
                {
                    "Fast" => 0,
                    "Medium" => 2,
                    "Long" => 3,
                    _ => 1
                };

                InitialSleepQuality = entry.SleepQuality;
                InitialDreamText = entry.DreamText ?? string.Empty;
                InitialDreamMoodIndex = entry.DreamMood switch
                {
                    "Positive" => 0,
                    "Negative" => 2,
                    "Nightmare" => 3,
                    _ => 1
                };
                InitialNotes = entry.Notes ?? string.Empty;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler",
                    $"Eintrag konnte nicht geladen werden: {ex.Message}", "OK");
            }
        }

        /// <summary>Wird vom Code-Behind mit den finalen UI-Werten aufgerufen.</summary>
        public async Task SaveFromViewAsync(
            DateTime bedTime,
            DateTime wakeTime,
            int quality,
            string category,
            string? dreamText,
            string? dreamMood,
            string? notes)
        {
            IsLoading = true;
            try
            {
                if (IsEditMode && EntryId.HasValue)
                {
                    var req = new UpdateSleepEntryRequest
                    {
                        Id = EntryId.Value,
                        BedTime = bedTime.ToUniversalTime(),
                        WakeTime = wakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = category,
                        SleepQuality = quality,
                        DreamText = dreamText,
                        DreamMood = dreamMood,
                        Notes = notes
                    };
                    if (await _sleepApiService.UpdateSleepEntryAsync(req) != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Eintrag aktualisiert ✓", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    var req = new AddSleepEntryRequest
                    {
                        BedTime = bedTime.ToUniversalTime(),
                        WakeTime = wakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = category,
                        SleepQuality = quality,
                        DreamText = dreamText,
                        DreamMood = dreamMood,
                        Notes = notes
                    };
                    if (await _sleepApiService.AddSleepEntryAsync(req) != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Nacht gespeichert! 🌙", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler",
                    $"Fehler beim Speichern: {ex.Message}", "OK");
            }
            finally { IsLoading = false; }
        }
    }
}