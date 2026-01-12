using Models;
using ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaterController : ControllerBase
    {
        private readonly DbManager _context;

        public WaterController(DbManager context)
        {
            _context = context;
        }

        // GET: api/water/today/{userId}
        [HttpGet("today/{userId}")]
        public async Task<ActionResult<List<WaterEntryDto>>> GetTodayEntries(int userId)
        {
            var today = DateTime.UtcNow.Date;
            var entries = await _context.WaterEntries
                .Where(w => w.UserId == userId && w.LoggedAt.Date == today)
                .OrderByDescending(w => w.LoggedAt)
                .Select(w => new WaterEntryDto
                {
                    Id = w.Id,
                    UserId = w.UserId,
                    AmountMl = w.AmountMl,
                    LoggedAt = w.LoggedAt,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(entries);
        }

        // POST: api/water/add
        [HttpPost("add")]
        public async Task<ActionResult<WaterEntryDto>> AddWater([FromBody] AddWaterRequest request, [FromQuery] int userId)
        {
            var entry = new WaterEntry
            {
                UserId = userId,
                AmountMl = request.AmountMl,
                LoggedAt = request.LoggedAt ?? DateTime.UtcNow
            };

            _context.WaterEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(new WaterEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                AmountMl = entry.AmountMl,
                LoggedAt = entry.LoggedAt,
                CreatedAt = entry.CreatedAt
            });
        }

        // PUT: api/water/update
        [HttpPut("update")]
        public async Task<ActionResult<WaterEntryDto>> UpdateWater([FromBody] UpdateWaterEntryRequest request, [FromQuery] int userId)
        {
            var entry = await _context.WaterEntries
                .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Eintrag nicht gefunden" });

            entry.AmountMl = request.AmountMl;
            if (request.LoggedAt.HasValue)
                entry.LoggedAt = request.LoggedAt.Value;

            await _context.SaveChangesAsync();

            return Ok(new WaterEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                AmountMl = entry.AmountMl,
                LoggedAt = entry.LoggedAt,
                CreatedAt = entry.CreatedAt
            });
        }

        // DELETE: api/water/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteWater(int id, [FromQuery] int userId)
        {
            var entry = await _context.WaterEntries
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Eintrag nicht gefunden" });

            _context.WaterEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Eintrag gelöscht" });
        }

        // GET: api/water/stats/week/{userId}
        [HttpGet("stats/week/{userId}")]
        public async Task<ActionResult<WeekStatsDto>> GetWeekStats(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            var goalMl = user.DailyWaterGoalMl ?? 2000;
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-6);

            var entries = await _context.WaterEntries
                .Where(w => w.UserId == userId && w.LoggedAt.Date >= weekAgo && w.LoggedAt.Date <= today)
                .ToListAsync();

            var dailyStats = new List<DailyStatsDto>();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayEntries = entries.Where(e => e.LoggedAt.Date == date);
                var totalMl = dayEntries.Sum(e => e.AmountMl);
                var percentage = goalMl > 0 ? (double)totalMl / goalMl * 100 : 0;

                dailyStats.Add(new DailyStatsDto
                {
                    Date = date,
                    TotalMl = totalMl,
                    GoalMl = goalMl,
                    Percentage = Math.Round(percentage, 1),
                    GoalReached = totalMl >= goalMl
                });
            }

            // Berechne Average
            var averageMl = dailyStats.Count > 0 ? (int)dailyStats.Average(d => d.TotalMl) : 0;

            // Berechne Streak
            int currentStreak = 0;
            int bestStreak = 0;
            int tempStreak = 0;

            // Current Streak (von heute rückwärts)
            for (int i = 6; i >= 0; i--)
            {
                if (dailyStats[i].GoalReached)
                {
                    if (i == 6) // Heute
                        currentStreak++;
                    else if (currentStreak > 0) // Nur weiterzählen wenn Streak aktiv
                        currentStreak++;
                    else
                        break; // Streak unterbrochen
                }
                else if (currentStreak > 0)
                {
                    break; // Streak endet
                }
            }

            // Best Streak (alle durchgehen)
            foreach (var day in dailyStats)
            {
                if (day.GoalReached)
                {
                    tempStreak++;
                    if (tempStreak > bestStreak)
                        bestStreak = tempStreak;
                }
                else
                {
                    tempStreak = 0;
                }
            }

            return Ok(new WeekStatsDto
            {
                Days = dailyStats,
                AverageMl = averageMl,
                CurrentStreak = currentStreak,
                BestStreak = bestStreak
            });
        }
    }
}