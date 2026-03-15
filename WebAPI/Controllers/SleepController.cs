using Models;
using ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SleepController : ControllerBase
    {
        private readonly DbManager _context;

        public SleepController(DbManager context)
        {
            _context = context;
        }

        // GET: api/sleep/recent/{userId}?days=7
        [HttpGet("recent/{userId}")]
        public async Task<ActionResult<List<SleepEntryDto>>> GetRecentEntries(int userId, [FromQuery] int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var entries = await _context.SleepEntries
                .Where(s => s.UserId == userId && s.BedTime.Date >= startDate)
                .OrderByDescending(s => s.BedTime)
                .Select(s => new SleepEntryDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    BedTime = s.BedTime,
                    WakeTime = s.WakeTime,
                    FallAsleepDurationCategory = s.FallAsleepDurationCategory,
                    SleepQuality = s.SleepQuality,
                    DreamText = s.DreamText,
                    DreamMood = s.DreamMood,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    TotalSleepHours = (s.WakeTime - s.BedTime).TotalHours
                })
                .ToListAsync();

            return Ok(entries);
        }

        // POST: api/sleep/add
        [HttpPost("add")]
        public async Task<ActionResult<SleepEntryDto>> AddSleepEntry([FromBody] AddSleepEntryRequest request, [FromQuery] int userId)
        {
            var entry = new SleepEntry
            {
                UserId = userId,
                BedTime = request.BedTime,
                WakeTime = request.WakeTime,
                FallAsleepDurationCategory = request.FallAsleepDurationCategory,
                SleepQuality = request.SleepQuality,
                DreamText = request.DreamText,
                DreamMood = request.DreamMood,
                Notes = request.Notes
            };

            _context.SleepEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(new SleepEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                BedTime = entry.BedTime,
                WakeTime = entry.WakeTime,
                FallAsleepDurationCategory = entry.FallAsleepDurationCategory,
                SleepQuality = entry.SleepQuality,
                DreamText = entry.DreamText,
                DreamMood = entry.DreamMood,
                Notes = entry.Notes,
                CreatedAt = entry.CreatedAt,
                TotalSleepHours = entry.TotalSleepHours
            });
        }

        // PUT: api/sleep/update
        [HttpPut("update")]
        public async Task<ActionResult<SleepEntryDto>> UpdateSleepEntry([FromBody] UpdateSleepEntryRequest request, [FromQuery] int userId)
        {
            var entry = await _context.SleepEntries
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Eintrag nicht gefunden" });

            entry.BedTime = request.BedTime;
            entry.WakeTime = request.WakeTime;
            entry.FallAsleepDurationCategory = request.FallAsleepDurationCategory;
            entry.SleepQuality = request.SleepQuality;
            entry.DreamText = request.DreamText;
            entry.DreamMood = request.DreamMood;
            entry.Notes = request.Notes;

            await _context.SaveChangesAsync();

            return Ok(new SleepEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                BedTime = entry.BedTime,
                WakeTime = entry.WakeTime,
                FallAsleepDurationCategory = entry.FallAsleepDurationCategory,
                SleepQuality = entry.SleepQuality,
                DreamText = entry.DreamText,
                DreamMood = entry.DreamMood,
                Notes = entry.Notes,
                CreatedAt = entry.CreatedAt,
                TotalSleepHours = entry.TotalSleepHours
            });
        }

        // DELETE: api/sleep/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteSleepEntry(int id, [FromQuery] int userId)
        {
            var entry = await _context.SleepEntries
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Eintrag nicht gefunden" });

            _context.SleepEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Eintrag gelöscht" });
        }


        // GET: api/sleep/stats/week/{userId}
        [HttpGet("stats/week/{userId}")]
        public async Task<ActionResult<WeekSleepStatsDto>> GetWeekStats(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User nicht gefunden" });

            var targetHours = user.TargetSleepHours;
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-6);

            // Letzte 7 Tage für die Tageskacheln
            var weekEntries = await _context.SleepEntries
                .Where(s => s.UserId == userId && s.BedTime.Date >= weekAgo && s.BedTime.Date <= today)
                .ToListAsync();

            var dailyStats = new List<DailySleepStatsDto>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayEntry = weekEntries.FirstOrDefault(e => e.BedTime.Date == date);

                if (dayEntry != null)
                {
                    dailyStats.Add(new DailySleepStatsDto
                    {
                        Date = date,
                        TotalSleepHours = dayEntry.TotalSleepHours,
                        TargetSleepHours = targetHours,
                        SleepQuality = dayEntry.SleepQuality,
                        GoalMet = dayEntry.TotalSleepHours >= targetHours,
                        Notes = dayEntry.Notes
                    });
                }
                else
                {
                    dailyStats.Add(new DailySleepStatsDto
                    {
                        Date = date,
                        TotalSleepHours = 0,
                        TargetSleepHours = targetHours,
                        SleepQuality = 0,
                        GoalMet = false
                    });
                }
            }

            // === STREAK: Alle Einträge laden, nicht nur letzte 7 Tage ===
            var allEntries = await _context.SleepEntries
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.BedTime)
                .ToListAsync();

            // Current Streak: von heute rückwärts, Tag für Tag prüfen
            int currentStreak = 0;
            var checkDate = today;

            // Erlaube auch gestern als Startpunkt (falls heute noch kein Eintrag)
            var hasToday = allEntries.Any(e => e.BedTime.Date == today && e.TotalSleepHours >= targetHours);
            var hasYesterday = allEntries.Any(e => e.BedTime.Date == today.AddDays(-1) && e.TotalSleepHours >= targetHours);

            if (!hasToday && !hasYesterday)
            {
                currentStreak = 0;
            }
            else
            {
                if (!hasToday)
                    checkDate = today.AddDays(-1); // Streak startet gestern

                while (true)
                {
                    var met = allEntries.Any(e => e.BedTime.Date == checkDate && e.TotalSleepHours >= targetHours);
                    if (met)
                    {
                        currentStreak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Best Streak: alle Einträge chronologisch durchgehen
            int bestStreak = 0;
            int tempStreak = 0;

            if (allEntries.Any())
            {
                var minDate = allEntries.Min(e => e.BedTime.Date);
                var cursor = minDate;

                while (cursor <= today)
                {
                    var met = allEntries.Any(e => e.BedTime.Date == cursor && e.TotalSleepHours >= targetHours);
                    if (met)
                    {
                        tempStreak++;
                        if (tempStreak > bestStreak)
                            bestStreak = tempStreak;
                    }
                    else
                    {
                        tempStreak = 0;
                    }
                    cursor = cursor.AddDays(1);
                }
            }

            var avgSleepHours = dailyStats.Where(d => d.TotalSleepHours > 0).Any()
                ? dailyStats.Where(d => d.TotalSleepHours > 0).Average(d => d.TotalSleepHours)
                : 0;

            var goalMetDays = dailyStats.Count(d => d.GoalMet);

            return Ok(new WeekSleepStatsDto
            {
                Days = dailyStats,
                AverageSleepHours = Math.Round(avgSleepHours, 1),
                TargetSleepHours = targetHours,
                GoalMetDays = goalMetDays,
                CurrentStreak = currentStreak,
                BestStreak = bestStreak
            });
        }

        // GET: api/sleep/stats/detailed/{userId}
        [HttpGet("stats/detailed/{userId}")]
        public async Task<ActionResult<SleepStatsDto>> GetDetailedStats(int userId, [FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var entries = await _context.SleepEntries
                .Where(s => s.UserId == userId && s.BedTime.Date >= startDate)
                .OrderByDescending(s => s.BedTime)
                .ToListAsync();

            if (!entries.Any())
            {
                return Ok(new SleepStatsDto
                {
                    AverageSleepHours = 0,
                    AverageFallAsleepMinutes = 0,
                    AverageSleepQuality = 0,
                    RecentEntries = new List<SleepEntryDto>()
                });
            }

            // Calculate averages
            var avgSleepHours = entries.Average(e => e.TotalSleepHours);
            var avgQuality = entries.Average(e => e.SleepQuality);

            // Average fall asleep minutes
            var fallAsleepMinutes = entries.Select(e => e.FallAsleepDurationCategory switch
            {
                "Fast" => 10.0,
                "Normal" => 22.5,
                "Medium" => 45.0,
                "Long" => 75.0,
                _ => 22.5
            }).Average();

            // Calculate average bed/wake times
            var avgBedMinutes = entries.Average(e => e.BedTime.Hour * 60 + e.BedTime.Minute);
            var avgWakeMinutes = entries.Average(e => e.WakeTime.Hour * 60 + e.WakeTime.Minute);

            // Best and worst nights
            var bestNight = entries.OrderByDescending(e => e.SleepQuality).ThenByDescending(e => e.TotalSleepHours).First();
            var worstNight = entries.OrderBy(e => e.SleepQuality).ThenBy(e => e.TotalSleepHours).First();

            // Dream statistics
            var dreamEntries = entries.Where(e => !string.IsNullOrEmpty(e.DreamText)).ToList();
            var dreamMoodCounts = dreamEntries
                .Where(e => !string.IsNullOrEmpty(e.DreamMood))
                .GroupBy(e => e.DreamMood!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Consistency score (based on variance in bed times)
            var bedTimeVariance = CalculateTimeVariance(entries.Select(e => e.BedTime).ToList());
            var consistencyScore = Math.Max(0, Math.Min(100, 100 - (int)(bedTimeVariance * 10)));

            return Ok(new SleepStatsDto
            {
                AverageSleepHours = Math.Round(avgSleepHours, 1),
                AverageFallAsleepMinutes = Math.Round(fallAsleepMinutes, 0),
                AverageSleepQuality = Math.Round(avgQuality, 1),
                AverageBedTime = TimeSpan.FromMinutes(avgBedMinutes),
                AverageWakeTime = TimeSpan.FromMinutes(avgWakeMinutes),
                ConsistencyScore = consistencyScore,
                BestNight = MapToDto(bestNight),
                WorstNight = MapToDto(worstNight),
                TotalDreams = dreamEntries.Count,
                DreamMoodCounts = dreamMoodCounts,
                RecentEntries = entries.Take(10).Select(MapToDto).ToList()
            });
        }

        private double CalculateTimeVariance(List<DateTime> times)
        {
            if (times.Count < 2) return 0;

            var minutes = times.Select(t => t.Hour * 60 + t.Minute).ToList();
            var mean = minutes.Average();
            var variance = minutes.Select(m => Math.Pow(m - mean, 2)).Average();
            return Math.Sqrt(variance) / 60.0; // Convert to hours
        }

        private SleepEntryDto MapToDto(SleepEntry entry)
        {
            return new SleepEntryDto
            {
                Id = entry.Id,
                UserId = entry.UserId,
                BedTime = entry.BedTime,
                WakeTime = entry.WakeTime,
                FallAsleepDurationCategory = entry.FallAsleepDurationCategory,
                SleepQuality = entry.SleepQuality,
                DreamText = entry.DreamText,
                DreamMood = entry.DreamMood,
                Notes = entry.Notes,
                CreatedAt = entry.CreatedAt,
                TotalSleepHours = entry.TotalSleepHours
            };
        }
    }
}