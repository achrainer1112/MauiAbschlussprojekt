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
        private string selectedFallAsleepCategory = "Normal";

        [ObservableProperty]
        private int sleepQuality = 3;

        [ObservableProperty]
        private string dreamText = string.Empty;

        [ObservableProperty]
        private string selectedDreamMood = "Neutral";

        [ObservableProperty]
        private string notes = string.Empty;

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

        [ObservableProperty]
        private double calculatedSleepHours;

        public List<string> FallAsleepCategories { get; } = new()
        {
            "Fast",
            "Normal",
            "Medium",
            "Long"
        };

        public List<string> DreamMoods { get; } = new()
        {
            "Positive",
            "Neutral",
            "Negative",
            "Nightmare"
        };

        public AddSleepEntryViewModel(SleepApiService sleepApiService)
        {
            _sleepApiService = sleepApiService;
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(BedTimeString) && DateTime.TryParse(BedTimeString, out var parsedBedTime))
            {
                BedTime = parsedBedTime;
            }

            if (!string.IsNullOrEmpty(WakeTimeString) && DateTime.TryParse(WakeTimeString, out var parsedWakeTime))
            {
                WakeTime = parsedWakeTime;
            }

            if (EntryId.HasValue)
            {
                IsEditMode = true;
                await LoadEntryAsync(EntryId.Value);
            }

            UpdateCalculatedSleepHours();
        }

        partial void OnBedTimeChanged(DateTime value)
        {
            UpdateCalculatedSleepHours();
        }

        partial void OnWakeTimeChanged(DateTime value)
        {
            UpdateCalculatedSleepHours();
        }

        private void UpdateCalculatedSleepHours()
        {
            var duration = WakeTime - BedTime;
            CalculatedSleepHours = Math.Round(duration.TotalHours, 1);
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
                    SelectedFallAsleepCategory = entry.FallAsleepDurationCategory;
                    SleepQuality = entry.SleepQuality;
                    DreamText = entry.DreamText ?? string.Empty;
                    SelectedDreamMood = entry.DreamMood ?? "Neutral";
                    Notes = entry.Notes ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Eintrag konnte nicht geladen werden: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (WakeTime <= BedTime)
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
                        BedTime = BedTime.ToUniversalTime(),
                        WakeTime = WakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = SelectedFallAsleepCategory,
                        SleepQuality = SleepQuality,
                        DreamText = string.IsNullOrWhiteSpace(DreamText) ? null : DreamText,
                        DreamMood = string.IsNullOrWhiteSpace(DreamText) ? null : SelectedDreamMood,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
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
                        BedTime = BedTime.ToUniversalTime(),
                        WakeTime = WakeTime.ToUniversalTime(),
                        FallAsleepDurationCategory = SelectedFallAsleepCategory,
                        SleepQuality = SleepQuality,
                        DreamText = string.IsNullOrWhiteSpace(DreamText) ? null : DreamText,
                        DreamMood = string.IsNullOrWhiteSpace(DreamText) ? null : SelectedDreamMood,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
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

        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}