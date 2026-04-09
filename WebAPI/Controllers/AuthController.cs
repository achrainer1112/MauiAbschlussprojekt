using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using ORM;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DbManager _context;
        private readonly JwtService _jwtService;
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthController(
            DbManager context,
            JwtService jwtService,
            PasswordService passwordService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _configuration = configuration;
        }

        // ── REGISTER ──────────────────────────────────────────────────────

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Benutzername, E-Mail und Passwort sind Pflichtfelder."
                });
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Diese E-Mail ist bereits registriert."
                });

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username);
            if (usernameExists)
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Dieser Benutzername ist bereits vergeben."
                });

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                WeightKg = request.WeightKg,
                ActivityLevel = request.ActivityLevel,
                IsActive = true
            };

            if (request.CustomDailyGoalMl.HasValue && request.CustomDailyGoalMl > 0)
            {
                user.DailyWaterGoalMl = request.CustomDailyGoalMl;
            }
            else if (request.WeightKg.HasValue &&
                     !string.IsNullOrEmpty(request.ActivityLevel))
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
            else
            {
                user.DailyWaterGoalMl = 2000;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Registrierung erfolgreich",
                User = MapToUserDto(user)
            });
        }

        // ── LOGIN ─────────────────────────────────────────────────────────

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "E-Mail und Passwort sind Pflichtfelder."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !user.IsActive)
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Ungültige Anmeldedaten."
                });

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Ungültige Anmeldedaten."
                });

            var token = _jwtService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                Message = "Login erfolgreich",
                User = MapToUserDto(user)
            });
        }

        // ── GOOGLE LOGIN ──────────────────────────────────────────────────

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> GoogleLogin(
            [FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Kein Token übermittelt."
                });

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[]
                    {
                        _configuration["Google:ClientId"],
                        _configuration["Google:AndroidClientId"]
                    }
                };

                var payload = await GoogleJsonWebSignature
                    .ValidateAsync(request.IdToken, settings);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    var baseUsername = payload.Name?
                        .Replace(" ", "")
                        .ToLower()
                        ?? payload.Email.Split('@')[0];

                    var username = baseUsername;
                    int suffix = 1;
                    while (await _context.Users.AnyAsync(u => u.Username == username))
                        username = $"{baseUsername}{suffix++}";

                    user = new User
                    {
                        Username = username,
                        Email = payload.Email,
                        PasswordHash = _passwordService.HashPassword(
                            Guid.NewGuid().ToString("N")),
                        IsActive = true,
                        DailyWaterGoalMl = 2000
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Google Login erfolgreich",
                    User = MapToUserDto(user)
                });
            }
            catch (InvalidJwtException ex)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = $"Ungültiges Google-Token: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = $"Serverfehler: {ex.Message}"
                });
            }
        }

        [HttpPost("google-exchange")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> GoogleExchange([FromBody] GoogleExchangeRequest request)
        {
            try
            {
                // Validierung
                if (string.IsNullOrWhiteSpace(request.Code))
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Authorization Code ist erforderlich"
                    });

                var clientId = _configuration["Google:ClientId"];
                var clientSecret = _configuration["Google:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    return StatusCode(500, new AuthResponse
                    {
                        Success = false,
                        Message = "Google-Konfiguration ist nicht vollständig"
                    });

                System.Diagnostics.Debug.WriteLine($"[Google Exchange] Code: {request.Code}");
                System.Diagnostics.Debug.WriteLine($"[Google Exchange] RedirectUri: {request.RedirectUri}");
                System.Diagnostics.Debug.WriteLine($"[Google Exchange] ClientId: {clientId}");

                // Token-Austausch serverseitig mit client_secret
                var parameters = new Dictionary<string, string>
                {
                    { "code",          request.Code         },
                    { "client_id",     clientId             },
                    { "client_secret", clientSecret         },
                    { "redirect_uri",  request.RedirectUri  },
                    { "grant_type",    "authorization_code" },
                    { "code_verifier", request.CodeVerifier }
                };

                var httpClient = new HttpClient();

                System.Diagnostics.Debug.WriteLine($"[Google Exchange] Sending request with redirect_uri: {request.RedirectUri}");
                System.Diagnostics.Debug.WriteLine($"[Google Exchange] Parameters: code={request.Code}, client_id={clientId}, grant_type=authorization_code");

                var tokenResponse = await httpClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(parameters));

                var json = await tokenResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Google Exchange] Response Status: {tokenResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[Google Exchange] Response: {json}");

                var tokenData = Newtonsoft.Json.JsonConvert
                    .DeserializeObject<Dictionary<string, string>>(json);

                if (tokenData == null ||
                    !tokenData.TryGetValue("id_token", out var idToken))
                {
                    var errorMsg = json ?? "Unknown error";
                    if (tokenData?.TryGetValue("error", out var error) == true)
                    {
                        errorMsg = $"Error: {error}";
                        if (tokenData.TryGetValue("error_description", out var desc))
                            errorMsg += $" - {desc}";
                    }

                    System.Diagnostics.Debug.WriteLine($"[Google Exchange] Error: {errorMsg}");

                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = $"Token-Austausch fehlgeschlagen: {errorMsg}"
                    });
                }

                // id_token validieren und User einloggen
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[]
                    {
                _configuration["Google:ClientId"],
                _configuration["Google:AndroidClientId"]
            }
                };

                var payload = await GoogleJsonWebSignature
                    .ValidateAsync(idToken, settings);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    var baseUsername = payload.Name?
                        .Replace(" ", "").ToLower()
                        ?? payload.Email.Split('@')[0];

                    var username = baseUsername;
                    int suffix = 1;
                    while (await _context.Users.AnyAsync(u => u.Username == username))
                        username = $"{baseUsername}{suffix++}";

                    user = new User
                    {
                        Username = username,
                        Email = payload.Email,
                        PasswordHash = _passwordService.HashPassword(
                            Guid.NewGuid().ToString("N")),
                        IsActive = true,
                        DailyWaterGoalMl = 2000
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                var jwtToken = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = jwtToken,
                    Message = "Google Login erfolgreich",
                    User = MapToUserDto(user)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = $"Fehler: {ex.Message}"
                });
            }
        }

        private static UserDto MapToUserDto(User user) => new UserDto
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