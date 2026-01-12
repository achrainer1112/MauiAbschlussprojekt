using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("username")]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Column("email")]
        public required string Email { get; set; }

        [Required]
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("weight_kg")]
        public double? WeightKg { get; set; }

        [Column("activity_level")]
        [MaxLength(20)]
        public string? ActivityLevel { get; set; }

        [Column("daily_water_goal_ml")]
        public int? DailyWaterGoalMl { get; set; }

        [Column("reminder_enabled")]
        public bool ReminderEnabled { get; set; } = false;

        [Column("reminder_interval_minutes")]
        public int ReminderIntervalMinutes { get; set; } = 60;

        [Column("reminder_start_hour")]
        public int ReminderStartHour { get; set; } = 8;

        [Column("reminder_end_hour")]
        public int ReminderEndHour { get; set; } = 22;

        // Navigation Property - das hat gefehlt!
        public ICollection<WaterEntry> WaterEntries { get; set; } = new List<WaterEntry>();
    }
}