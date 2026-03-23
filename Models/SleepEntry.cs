using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("sleep_entries")]
    public class SleepEntry
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("bed_time")]
        public DateTime BedTime { get; set; }

        [Required]
        [Column("wake_time")]
        public DateTime WakeTime { get; set; }

        [Column("fall_asleep_duration_category")]
        [MaxLength(20)]
        public string FallAsleepDurationCategory { get; set; } = "Normal"; // Fast, Normal, Medium, Long

        [Column("sleep_quality")]
        public int SleepQuality { get; set; } = 3; // 1-5 stars

        [Column("dream_text")]
        public string? DreamText { get; set; }

        [Column("dream_mood")]
        [MaxLength(20)]
        public string? DreamMood { get; set; } // Positive, Neutral, Negative, Nightmare

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("UserId")]
        public User User { get; set; } = null!;


        [NotMapped]
        public TimeSpan TotalSleepDuration => WakeTime - BedTime;

        [NotMapped]
        public double TotalSleepHours => TotalSleepDuration.TotalHours;
    }
}
