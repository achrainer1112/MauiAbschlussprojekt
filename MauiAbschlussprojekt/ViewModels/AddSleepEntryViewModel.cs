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

        // ── Separate Date/Time Properties für DatePicker + TimePicker ─────────
        // MAUI DatePicker bindet an DateTime, TimePicker an TimeSpan.
        // Wir halten Datum und Uhrzeit getrennt und kombinieren sie in BedTime/WakeTime.

        [ObservableProperty] private DateTime bedDate = DateTime.Today;
        [ObservableProperty] private TimeSpan bedTimeSpan = new TimeSpan(23, 0, 0);

        [ObservableProperty] private DateTime wakeDate = DateTime.Today.AddDays(1);
        [ObservableProperty] private TimeSpan wakeTimeSpan = new TimeSpan(7, 0, 0);

        // Kombinierte DateTime-Properties (berechnet aus Date + TimeSpan)
        public DateTime BedTime => bedDate.Date + bedTimeSpan;
        public DateTime WakeTime => wakeDate.Date + wakeTimeSpan;

        [ObservableProperty] private string selectedFallAsleepCategory = "Normal (15–30 Min)";
        [ObservableProperty] private int sleepQuality = 3;
        [ObservableProperty] private string dreamText = string.Empty;
        [ObservableProperty] private string selectedDreamMood = "Neutral";
        [ObservableProperty] private string notes = string.Empty;
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEditMode;
        [ObservableProperty] private int? entryId;
        [ObservableProperty] private string bedTimeString = string.Empty;
        [ObservableProperty] private string wakeTimeString = string.Empty;
        [ObservableProperty] private double calculatedSleepHours;

        // Anzeige der berechneten Schlafdauer mit Warnung bei negativem Wert
        public string SleepDurationWarning =>
            CalculatedSleepHours < 0
                ? "⚠️ Aufwachzeit liegt vor Einschlafzeit!"
                : CalculatedSleepHours > 24
                    ? "⚠️ Schlafdauer über 24 Stunden – bitte prüfen"
                    : string.Empty;

        public bool HasWarning => !string.IsNullOrEmpty(SleepDurationWarning);
        public bool HasDreamText => !string.IsNullOrWhiteSpace(DreamText);

        // ── Listen ────────────────────────────────────────────────────────────
        public List<string> FallAsleepCategories { get; } = new()
        {
            "Schnell (unter 15 Min)",
            "Normal (15–30 Min)",
            "Mittel (30–60 Min)",
            "Lang (über 60 Min)"
        };

        public List<string> DreamMoods { get; } = new()
        {
            "Positiv",
            "Neutral",
            "Negativ",
            "Albtraum"
        };

        public AddSleepEntryViewModel(SleepApiService sleepApiService)
        {
            _sleepApiService = sleepApiService;
        }

        public async Task InitializeAsync()
        {
            // QueryProperty-Werte verarbeiten (kommen vom QuickLog)
            if (!string.IsNullOrEmpty(BedTimeString) && DateTime.TryParse(BedTimeString, out var parsedBed))
            {
                BedDate = parsedBed.Date;
                BedTimeSpan = parsedBed.TimeOfDay;
            }

            if (!string.IsNullOrEmpty(WakeTimeString) && DateTime.TryParse(WakeTimeString, out var parsedWake))
            {
                WakeDate = parsedWake.Date;
                WakeTimeSpan = parsedWake.TimeOfDay;
            }

            if (EntryId.HasValue)
            {
                IsEditMode = true;
                await LoadEntryAsync(EntryId.Value);
            }

            UpdateCalculatedSleepHours();
        }

        // ── Property-Changed Hooks ────────────────────────────────────────────
        partial void OnDreamTextChanged(string value) => OnPropertyChanged(nameof(HasDreamText));
        partial void OnBedDateChanged(DateTime value) => UpdateCalculatedSleepHours();
        partial void OnBedTimeSpanChanged(TimeSpan value) => UpdateCalculatedSleepHours();
        partial void OnWakeDateChanged(DateTime value) => UpdateCalculatedSleepHours();
        partial void OnWakeTimeSpanChanged(TimeSpan value) => UpdateCalculatedSleepHours();

        private void UpdateCalculatedSleepHours()
        {
            var duration = WakeTime - BedTime;
            CalculatedSleepHours = Math.Round(duration.TotalHours, 1);
            OnPropertyChanged(nameof(BedTime));
            OnPropertyChanged(nameof(WakeTime));
            OnPropertyChanged(nameof(SleepDurationWarning));
            OnPropertyChanged(nameof(HasWarning));
        }

        // ── Eintrag laden (Edit-Modus) ────────────────────────────────────────
        private async Task LoadEntryAsync(int id)
        {
            try
            {
                var entries = await _sleepApiService.GetRecentEntriesAsync(30);
                var entry = entries.FirstOrDefault(e => e.Id == id);

                if (entry != null)
                {
                    var localBed = entry.BedTime.ToLocalTime();
                    var localWake = entry.WakeTime.ToLocalTime();

                    BedDate = localBed.Date;
                    BedTimeSpan = localBed.TimeOfDay;
                    WakeDate = localWake.Date;
                    WakeTimeSpan = localWake.TimeOfDay;

                    // API-Kategorie (englisch) → deutsch mappen
                    SelectedFallAsleepCategory = entry.FallAsleepDurationCategory switch
                    {
                        "Fast" => "Schnell (unter 15 Min)",
                        "Normal" => "Normal (15–30 Min)",
                        "Medium" => "Mittel (30–60 Min)",
                        "Long" => "Lang (über 60 Min)",
                        _ => "Normal (15–30 Min)"
                    };

                    SleepQuality = entry.SleepQuality;
                    DreamText = entry.DreamText ?? string.Empty;
                    SelectedDreamMood = entry.DreamMood switch
                    {
                        "Positive" => "Positiv",
                        "Negative" => "Negativ",
                        "Nightmare" => "Albtraum",
                        _ => "Neutral"
                    };
                    Notes = entry.Notes ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Eintrag konnte nicht geladen werden: {ex.Message}", "OK");
            }
        }

        // ── Speichern ─────────────────────────────────────────────────────────
        [RelayCommand]
        private async Task SaveAsync()
        {
            // Validierung
            if (WakeTime <= BedTime)
            {
                // Automatisch Korrektur: WakeDate auf nächsten Tag setzen
                bool fix = await Shell.Current.DisplayAlert(
                    "Datum prüfen",
                    $"Aufwachzeit ({WakeTime:HH:mm}) liegt vor oder gleich Einschlafzeit ({BedTime:HH:mm}).\n" +
                    "Soll das Aufwachdatum automatisch auf den nächsten Tag gesetzt werden?",
                    "Ja, korrigieren",
                    "Abbrechen");

                if (!fix) return;

                WakeDate = BedDate.AddDays(1);
                if (WakeTime <= BedTime)
                {
                    await Shell.Current.DisplayAlert("Fehler", "Aufwachzeit muss nach der Einschlafzeit liegen.", "OK");
                    return;
                }
            }

            if (CalculatedSleepHours > 24)
            {
                bool proceed = await Shell.Current.DisplayAlert(
                    "Ungewöhnliche Schlafdauer",
                    $"Die Schlafdauer beträgt {CalculatedSleepHours:F1} Stunden. Trotzdem speichern?",
                    "Ja", "Abbrechen");
                if (!proceed) return;
            }

            // Deutsch → Englisch für API
            string apiCategory = SelectedFallAsleepCategory switch
            {
                "Schnell (unter 15 Min)" => "Fast",
                "Mittel (30–60 Min)" => "Medium",
                "Lang (über 60 Min)" => "Long",
                _ => "Normal"
            };

            string? apiDreamMood = string.IsNullOrWhiteSpace(DreamText) ? null : SelectedDreamMood switch
            {
                "Positiv" => "Positive",
                "Negativ" => "Negative",
                "Albtraum" => "Nightmare",
                _ => "Neutral"
            };

            IsLoading = true;
            try
            {
                if (IsEditMode && EntryId.HasValue)
                {
                    var request = new UpdateSleepEntryRequest
                    {
                        Id = EntryId.Value,
                        BedTime = BedTime.ToUniversalTime(),
                        WakeTime = WakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = apiCategory,
                        SleepQuality = SleepQuality,
                        DreamText = string.IsNullOrWhiteSpace(DreamText) ? null : DreamText,
                        DreamMood = apiDreamMood,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
                    };

                    if (await _sleepApiService.UpdateSleepEntryAsync(request) != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Eintrag aktualisiert", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    var request = new AddSleepEntryRequest
                    {
                        BedTime = BedTime.ToUniversalTime(),
                        WakeTime = WakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = apiCategory,
                        SleepQuality = SleepQuality,
                        DreamText = string.IsNullOrWhiteSpace(DreamText) ? null : DreamText,
                        DreamMood = apiDreamMood,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
                    };

                    if (await _sleepApiService.AddSleepEntryAsync(request) != null)
                    {
                        await Shell.Current.DisplayAlert("Erfolg", "Nacht gespeichert!", "OK");
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