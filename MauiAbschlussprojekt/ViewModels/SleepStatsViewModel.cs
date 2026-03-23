using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class SleepStatsViewModel : ObservableObject
    {
        private readonly SleepApiService _sleepApiService;

        [ObservableProperty]
        private double averageSleepHours;

        [ObservableProperty]
        private double targetSleepHours;

        [ObservableProperty]
        private int currentStreak;

        [ObservableProperty]
        private int bestStreak;

        [ObservableProperty]
        private int goalMetDays;

        [ObservableProperty]
        private double averageFallAsleepMinutes;

        [ObservableProperty]
        private double averageSleepQuality;

        [ObservableProperty]
        private int consistencyScore;

        [ObservableProperty]
        private int totalDreams;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private SleepEntryDto? bestNight;

        [ObservableProperty]
        private SleepEntryDto? worstNight;


        public string GoalMetDaysText => $"{GoalMetDays}/7 Tage";
        public string ConsistencyScoreText => $"{ConsistencyScore}%";
        public string AverageFallAsleepText => AverageFallAsleepMinutes > 0
            ? $"~{(int)AverageFallAsleepMinutes} Min"
            : "–";

        public ObservableCollection<DailySleepStatsDto> WeekStats { get; } = new();
        public ObservableCollection<DreamMoodCount> DreamMoodStats { get; } = new();
        public ObservableCollection<SleepEntryDto> RecentEntries { get; } = new();
        public ObservableCollection<SleepEntryDto> DreamEntries { get; } = new();

        public SleepStatsViewModel(SleepApiService sleepApiService)
        {
            _sleepApiService = sleepApiService;
        }

        public async Task InitializeAsync()
        {
            await LoadStatsAsync();
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            IsLoading = true;

            try
            {
                var weekStats = await _sleepApiService.GetWeekStatsAsync();

                if (weekStats != null)
                {
                    WeekStats.Clear();
                    foreach (var day in weekStats.Days.OrderByDescending(d => d.Date))
                        WeekStats.Add(day);

                    AverageSleepHours = weekStats.AverageSleepHours;
                    TargetSleepHours = weekStats.TargetSleepHours;
                    CurrentStreak = weekStats.CurrentStreak;
                    BestStreak = weekStats.BestStreak;
                    GoalMetDays = weekStats.GoalMetDays; 
                }


                var detailedStats = await _sleepApiService.GetDetailedStatsAsync(30);

                if (detailedStats != null)
                {
                    AverageFallAsleepMinutes = detailedStats.AverageFallAsleepMinutes;
                    AverageSleepQuality = detailedStats.AverageSleepQuality;
                    ConsistencyScore = detailedStats.ConsistencyScore;
                    TotalDreams = detailedStats.TotalDreams;
                    BestNight = detailedStats.BestNight;
                    WorstNight = detailedStats.WorstNight;

                    RecentEntries.Clear();
                    DreamEntries.Clear();

                    foreach (var entry in detailedStats.RecentEntries.OrderByDescending(e => e.BedTime))
                    {
                        RecentEntries.Add(entry);

                        if (!string.IsNullOrWhiteSpace(entry.DreamText))
                            DreamEntries.Add(entry);
                    }

                    DreamMoodStats.Clear();
                    foreach (var mood in detailedStats.DreamMoodCounts)
                    {
                        DreamMoodStats.Add(new DreamMoodCount
                        {
                            Mood = mood.Key,
                            Count = mood.Value
                        });
                    }
                }


                OnPropertyChanged(nameof(GoalMetDaysText));
                OnPropertyChanged(nameof(ConsistencyScoreText));
                OnPropertyChanged(nameof(AverageFallAsleepText));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Statistiken konnten nicht geladen werden: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteEntryAsync(SleepEntryDto entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Eintrag löschen",
                $"Nacht vom {entry.BedTime:dd.MM.yyyy} wirklich löschen?",
                "Ja, löschen", "Abbrechen");

            if (!confirm) return;

            try
            {
                bool success = await _sleepApiService.DeleteSleepEntryAsync(entry.Id);
                if (success)
                {
                    RecentEntries.Remove(entry);
                    DreamEntries.Remove(entry);
                    await LoadStatsAsync();
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
        private async Task EditEntryAsync(SleepEntryDto entry)
        {
            await Shell.Current.GoToAsync($"AddSleepEntryPage?entryId={entry.Id}");
        }

        [RelayCommand]
        private async Task ViewDreamAsync(SleepEntryDto entry)
        {
            if (string.IsNullOrWhiteSpace(entry.DreamText))
                return;

            string moodEmoji = entry.DreamMood switch
            {
                "Positive" => "😊",
                "Neutral" => "😐",
                "Negative" => "😟",
                "Nightmare" => "😱",
                _ => "💭"
            };

            string title = $"Traum vom {entry.BedTime:dd.MM.yyyy} {moodEmoji}";
            await Shell.Current.DisplayAlert(title, entry.DreamText, "Schließen");
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public class DreamMoodCount
    {
        public string Mood { get; set; } = string.Empty;
        public int Count { get; set; }
        public string DisplayText => $"{GetMoodEmoji(Mood)} {GetMoodText(Mood)}: {Count}";

        private string GetMoodEmoji(string mood) => mood switch
        {
            "Positive" => "😊",
            "Neutral" => "😐",
            "Negative" => "😟",
            "Nightmare" => "😱",
            _ => "❓"
        };

        private string GetMoodText(string mood) => mood switch
        {
            "Positive" => "Positiv",
            "Neutral" => "Neutral",
            "Negative" => "Negativ",
            "Nightmare" => "Albtraum",
            _ => "Unbekannt"
        };
    }
}