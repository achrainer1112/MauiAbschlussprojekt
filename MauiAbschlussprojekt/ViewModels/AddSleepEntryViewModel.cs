using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        [ObservableProperty]
        private DateTime bedTime = DateTime.Now.AddHours(-8);

        [ObservableProperty]
        private DateTime wakeTime = DateTime.Now;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private int? entryId;

        [ObservableProperty]
        private string bedTimeString = string.Empty;

        [ObservableProperty]
        private string wakeTimeString = string.Empty;

        // ── Initial-Werte für den Code-Behind ──────────────────────────────
        // Werden nach InitializeAsync() von OnAppearing() ausgelesen

        public int InitialSleepQuality { get; private set; } = 3;

        /// <summary>Index in der Picker-Liste: Fast=0, Normal=1, Medium=2, Long=3</summary>
        public int InitialFallAsleepIndex { get; private set; } = 1;

        public string InitialDreamText { get; private set; } = string.Empty;

        /// <summary>Index in der Picker-Liste: Positive=0, Neutral=1, Negative=2, Nightmare=3</summary>
        public int InitialDreamMoodIndex { get; private set; } = 1;

        public string InitialNotes { get; private set; } = string.Empty;

        // ───────────────────────────────────────────────────────────────────

        public AddSleepEntryViewModel(SleepApiService sleepApiService)
        {
            _sleepApiService = sleepApiService;
        }

        public async Task InitializeAsync()
        {
            // Zeiten aus Query-Parametern parsen (Format "yyyyMMddHHmm" vom QuickLog)
            if (!string.IsNullOrEmpty(BedTimeString) && TryParseDateTime(BedTimeString, out var parsedBed))
                BedTime = parsedBed;

            if (!string.IsNullOrEmpty(WakeTimeString) && TryParseDateTime(WakeTimeString, out var parsedWake))
                WakeTime = parsedWake;

            if (EntryId.HasValue)
            {
                IsEditMode = true;
                await LoadEntryAsync(EntryId.Value);
            }
        }

        private bool TryParseDateTime(string value, out DateTime result)
        {
            // Kompaktes URL-sicheres Format vom QuickLog: "yyyyMMddHHmm"
            if (DateTime.TryParseExact(value, "yyyyMMddHHmm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result))
                return true;

            // Fallback für andere Formate
            return DateTime.TryParse(value, out result);
        }

        private async Task LoadEntryAsync(int id)
        {
            try
            {
                var entries = await _sleepApiService.GetRecentEntriesAsync(30);
                var entry = entries.FirstOrDefault(e => e.Id == id);

                if (entry != null)
                {
                    BedTime = entry.BedTime.ToLocalTime();
                    WakeTime = entry.WakeTime.ToLocalTime();

                    InitialSleepQuality = entry.SleepQuality;

                    InitialFallAsleepIndex = entry.FallAsleepDurationCategory switch
                    {
                        "Fast" => 0,
                        "Medium" => 2,
                        "Long" => 3,
                        _ => 1  // Normal
                    };

                    InitialDreamText = entry.DreamText ?? string.Empty;

                    InitialDreamMoodIndex = entry.DreamMood switch
                    {
                        "Positive" => 0,
                        "Negative" => 2,
                        "Nightmare" => 3,
                        _ => 1  // Neutral
                    };

                    InitialNotes = entry.Notes ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Eintrag konnte nicht geladen werden: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Wird vom Code-Behind aufgerufen wenn der Save-Button gedrückt wird.
        /// Alle Werte kommen direkt aus den UI-Controls (nicht per Binding).
        /// </summary>
        public async Task SaveFromViewAsync(
            DateTime bedTime,
            DateTime wakeTime,
            int quality,
            string category,
            string? dreamText,
            string? dreamMood,
            string? notes)
        {
            if (wakeTime <= bedTime)
            {
                await Shell.Current.DisplayAlert("Fehler", "Aufwachzeit muss nach der Einschlafzeit liegen", "OK");
                return;
            }

            IsLoading = true;
            try
            {
                if (IsEditMode && EntryId.HasValue)
                {
                    var request = new UpdateSleepEntryRequest
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

                    var result = await _sleepApiService.UpdateSleepEntryAsync(request);
                    if (result != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Eintrag aktualisiert", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    var request = new AddSleepEntryRequest
                    {
                        BedTime = bedTime.ToUniversalTime(),
                        WakeTime = wakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = category,
                        SleepQuality = quality,
                        DreamText = dreamText,
                        DreamMood = dreamMood,
                        Notes = notes
                    };

                    var result = await _sleepApiService.AddSleepEntryAsync(request);
                    if (result != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Eintrag gespeichert", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
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
    }
}