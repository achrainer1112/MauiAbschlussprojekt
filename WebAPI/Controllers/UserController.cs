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

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            return Ok(MapToUserDto(user));
        }

        [HttpPut("update")]
        public async Task<ActionResult<UserDto>> UpdateUser([FromBody] UpdateUserRequest request, [FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            // Username aktualisieren (neu)
            if (!string.IsNullOrWhiteSpace(request.Username))
                user.Username = request.Username;

            if (request.WeightKg.HasValue)
                user.WeightKg = request.WeightKg;

            if (!string.IsNullOrEmpty(request.ActivityLevel))
                user.ActivityLevel = request.ActivityLevel;

            if (request.DailyWaterGoalMl.HasValue)
            {
                user.DailyWaterGoalMl = request.DailyWaterGoalMl;
            }
            else if (request.WeightKg.HasValue && !string.IsNullOrEmpty(request.ActivityLevel))
            {
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

            return Ok(MapToUserDto(user));
        }

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

            return Ok(MapToUserDto(user));
        }

        [HttpPut("sleep-settings")]
        public async Task<ActionResult<UserDto>> UpdateSleepSettings([FromBody] UpdateSleepSettingsRequest request, [FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            if (request.TargetSleepHours.HasValue)
                user.TargetSleepHours = request.TargetSleepHours.Value;

            if (request.TargetBedTimeHour.HasValue)
                user.TargetBedTimeHour = request.TargetBedTimeHour.Value;

            if (request.TargetBedTimeMinute.HasValue)
                user.TargetBedTimeMinute = request.TargetBedTimeMinute.Value;

            if (request.TargetWakeTimeHour.HasValue)
                user.TargetWakeTimeHour = request.TargetWakeTimeHour.Value;

            if (request.TargetWakeTimeMinute.HasValue)
                user.TargetWakeTimeMinute = request.TargetWakeTimeMinute.Value;

            if (request.SleepReminderEnabled.HasValue)
                user.SleepReminderEnabled = request.SleepReminderEnabled.Value;

            await _context.SaveChangesAsync();

            return Ok(MapToUserDto(user));
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
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
                ReminderEndHour = user.ReminderEndHour,
                TargetSleepHours = user.TargetSleepHours,
                TargetBedTimeHour = user.TargetBedTimeHour,
                TargetBedTimeMinute = user.TargetBedTimeMinute,
                TargetWakeTimeHour = user.TargetWakeTimeHour,
                TargetWakeTimeMinute = user.TargetWakeTimeMinute,
                SleepReminderEnabled = user.SleepReminderEnabled
            };
        }
    }
}