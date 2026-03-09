using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Sleep Entry DTOs
    public class SleepEntryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime BedTime { get; set; }
        public DateTime WakeTime { get; set; }
        public string FallAsleepDurationCategory { get; set; } = "Normal";
        public int SleepQuality { get; set; }
        public string? DreamText { get; set; }
        public string? DreamMood { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public double TotalSleepHours { get; set; }
    }

    public class AddSleepEntryRequest
    {
        [Required]
        public DateTime BedTime { get; set; }

        [Required]
        public DateTime WakeTime { get; set; }

        public string FallAsleepDurationCategory { get; set; } = "Normal";
        public int SleepQuality { get; set; } = 3;
        public string? DreamText { get; set; }
        public string? DreamMood { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateSleepEntryRequest
    {
        public int Id { get; set; }
        public DateTime BedTime { get; set; }
        public DateTime WakeTime { get; set; }
        public string FallAsleepDurationCategory { get; set; } = "Normal";
        public int SleepQuality { get; set; }
        public string? DreamText { get; set; }
        public string? DreamMood { get; set; }
        public string? Notes { get; set; }
    }

    // Sleep Statistics
    public class SleepStatsDto
    {
        public double AverageSleepHours { get; set; }
        public double AverageFallAsleepMinutes { get; set; }
        public double AverageSleepQuality { get; set; }
        public TimeSpan AverageBedTime { get; set; }
        public TimeSpan AverageWakeTime { get; set; }
        public int ConsistencyScore { get; set; }
        public SleepEntryDto? BestNight { get; set; }
        public SleepEntryDto? WorstNight { get; set; }
        public int TotalDreams { get; set; }
        public Dictionary<string, int> DreamMoodCounts { get; set; } = new();
        public List<SleepEntryDto> RecentEntries { get; set; } = new();
    }

    public class WeekSleepStatsDto
    {
        public List<DailySleepStatsDto> Days { get; set; } = new();
        public double AverageSleepHours { get; set; }
        public double TargetSleepHours { get; set; }
        public int GoalMetDays { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
    }

    public class DailySleepStatsDto
    {
        public DateTime Date { get; set; }
        public double TotalSleepHours { get; set; }
        public double TargetSleepHours { get; set; }
        public int SleepQuality { get; set; }
        public bool GoalMet { get; set; }
        public string? Notes { get; set; }

        // Berechneter Fortschrittswert für ProgressBar (0.0 – 1.0)
        public double ProgressValue => TargetSleepHours > 0
            ? Math.Min(TotalSleepHours / TargetSleepHours, 1.0)
            : 0.0;

        // Anzeigetext für Ziel-Status
        public string GoalStatusText => GoalMet ? "✅ Ziel erreicht" : (TotalSleepHours > 0 ? "❌ Ziel nicht erreicht" : "– Kein Eintrag");

        // Farbe für ProgressBar
        public string ProgressColor => GoalMet ? "#27AE60" : (TotalSleepHours > 0 ? "#E67E22" : "#BDC3C7");
    }

    public class DreamMoodCount
    {
        public string Mood { get; set; } = string.Empty;
        public int Count { get; set; }
        public string DisplayText => $"{Mood}: {Count}x";
    }


    public class UpdateSleepSettingsRequest
    {
        public double? TargetSleepHours { get; set; }
        public int? TargetBedTimeHour { get; set; }
        public int? TargetBedTimeMinute { get; set; }
        public int? TargetWakeTimeHour { get; set; }
        public int? TargetWakeTimeMinute { get; set; }
        public bool? SleepReminderEnabled { get; set; }
    }
}