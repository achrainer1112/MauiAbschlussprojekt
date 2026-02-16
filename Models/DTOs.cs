using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username ist erforderlich")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username muss zwischen 3 und 100 Zeichen lang sein")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein")]
        public string Password { get; set; } = string.Empty;

        public double? WeightKg { get; set; }
        public string? ActivityLevel { get; set; }
        public int? CustomDailyGoalMl { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? WeightKg { get; set; }
        public string? ActivityLevel { get; set; }
        public int? DailyWaterGoalMl { get; set; }
        public bool ReminderEnabled { get; set; }
        public int ReminderIntervalMinutes { get; set; }
        public int ReminderStartHour { get; set; }
        public int ReminderEndHour { get; set; }
    }

    public class UpdateUserRequest
    {
        public double? WeightKg { get; set; }
        public string? ActivityLevel { get; set; }
        public int? DailyWaterGoalMl { get; set; }
    }

    public class UpdateReminderRequest
    {
        public bool ReminderEnabled { get; set; }
        public int? ReminderIntervalMinutes { get; set; }
        public int? ReminderStartHour { get; set; }
        public int? ReminderEndHour { get; set; }
    }

    public class WaterEntryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AmountMl { get; set; }
        public DateTime LoggedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddWaterRequest
    {
        public int AmountMl { get; set; }
        public DateTime? LoggedAt { get; set; }
    }

    public class UpdateWaterEntryRequest
    {
        public int Id { get; set; }
        public int AmountMl { get; set; }
        public DateTime? LoggedAt { get; set; }
    }

    public class DailyStatsDto
    {
        public DateTime Date { get; set; }
        public int TotalMl { get; set; }
        public int GoalMl { get; set; }
        public double Percentage { get; set; }
        public bool GoalReached { get; set; }
        public double ProgressValue => Percentage / 100.0;
    }


    public class WeekStatsDto
    {
        public List<DailyStatsDto> Days { get; set; } = new();
        public int AverageMl { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
    }
}