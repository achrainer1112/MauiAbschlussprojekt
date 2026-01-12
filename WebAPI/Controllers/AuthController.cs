using Models;
using ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DbManager _context;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;

        public AuthController(DbManager context, PasswordService passwordService, JwtService jwtService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return Ok(new AuthResponse
                    {
                        Success = false,
                        Message = "Username bereits vergeben"
                    });
                }

                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return Ok(new AuthResponse
                    {
                        Success = false,
                        Message = "Email bereits registriert"
                    });
                }

                int? calculatedGoal = null;
                if (request.WeightKg.HasValue)
                {
                    double multiplier = request.ActivityLevel switch
                    {
                        "low" => 30,        // = 30ml pro kg
                        "medium" => 35,
                        "high" => 40,
                        _ => 33
                    };
                    calculatedGoal = (int)(request.WeightKg.Value * multiplier);
                }

                int? finalGoal = request.CustomDailyGoalMl ?? calculatedGoal;

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    WeightKg = request.WeightKg,
                    ActivityLevel = request.ActivityLevel,
                    DailyWaterGoalMl = finalGoal
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Registrierung erfolgreich",
                    Token = token,
                    User = new UserDto
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
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new AuthResponse
                {
                    Success = false,
                    Message = $"Fehler: {ex.Message}"
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Ok(new AuthResponse
                    {
                        Success = false,
                        Message = "Ungültige Anmeldedaten"
                    });
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Login erfolgreich",
                    Token = token,
                    User = new UserDto
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
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new AuthResponse
                {
                    Success = false,
                    Message = $"Fehler: {ex.Message}"
                });
            }
        }
    }
}