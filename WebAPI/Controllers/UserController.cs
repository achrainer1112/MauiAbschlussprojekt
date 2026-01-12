using Models;
using ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DbManager _context;

        public UserController(DbManager context)
        {
            _context = context;
        }

        // GET: api/user/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                WeightKg = user.WeightKg,
                ActivityLevel = user.ActivityLevel,
                DailyWaterGoalMl = user.DailyWaterGoalMl,
                ReminderEnabled = user.ReminderEnabled,
                ReminderIntervalMinutes = user.ReminderIntervalMinutes,
                ReminderStartHour = user.ReminderStartHour,
                ReminderEndHour = user.ReminderEndHour
            });
        }

        // PUT: api/user/update
        [HttpPut("update")]
        public async Task<ActionResult<UserDto>> UpdateUser([FromBody] UpdateUserRequest request, [FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            // Update Gewicht und Aktivitätslevel
            if (request.WeightKg.HasValue)
                user.WeightKg = request.WeightKg;

            if (!string.IsNullOrEmpty(request.ActivityLevel))
                user.ActivityLevel = request.ActivityLevel;

            // Update Wasserziel
            if (request.DailyWaterGoalMl.HasValue)
            {
                user.DailyWaterGoalMl = request.DailyWaterGoalMl;
            }
            else if (request.WeightKg.HasValue && !string.IsNullOrEmpty(request.ActivityLevel))
            {
                // Automatische Berechnung wenn kein Custom Goal angegeben
                double multiplier = request.ActivityLevel switch
                {
                    "low" => 30,
                    "medium" => 35,
                    "high" => 40,
                    _ => 33
                };
                user.DailyWaterGoalMl = (int)(request.WeightKg.Value * multiplier);
            }

            await _context.SaveChangesAsync();

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                WeightKg = user.WeightKg,
                ActivityLevel = user.ActivityLevel,
                DailyWaterGoalMl = user.DailyWaterGoalMl,
                ReminderEnabled = user.ReminderEnabled,
                ReminderIntervalMinutes = user.ReminderIntervalMinutes,
                ReminderStartHour = user.ReminderStartHour,
                ReminderEndHour = user.ReminderEndHour
            });
        }

        // PUT: api/user/reminder-settings
        [HttpPut("reminder-settings")]
        public async Task<ActionResult<UserDto>> UpdateReminderSettings([FromBody] UpdateReminderRequest request, [FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            user.ReminderEnabled = request.ReminderEnabled;

            if (request.ReminderIntervalMinutes.HasValue)
                user.ReminderIntervalMinutes = request.ReminderIntervalMinutes.Value;

            if (request.ReminderStartHour.HasValue)
                user.ReminderStartHour = request.ReminderStartHour.Value;

            if (request.ReminderEndHour.HasValue)
                user.ReminderEndHour = request.ReminderEndHour.Value;

            await _context.SaveChangesAsync();

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                WeightKg = user.WeightKg,
                ActivityLevel = user.ActivityLevel,
                DailyWaterGoalMl = user.DailyWaterGoalMl,
                ReminderEnabled = user.ReminderEnabled,
                ReminderIntervalMinutes = user.ReminderIntervalMinutes,
                ReminderStartHour = user.ReminderStartHour,
                ReminderEndHour = user.ReminderEndHour
            });
        }
    }
}

// DTOs für User-Updates (zu DTOs.cs hinzufügen)
namespace Models
{
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
    }

    public class WeekStatsDto
    {
        public List<DailyStatsDto> Days { get; set; } = new();
        public int AverageMl { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
    }
}