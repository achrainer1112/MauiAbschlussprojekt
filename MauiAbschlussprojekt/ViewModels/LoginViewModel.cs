using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IReminderService _reminderService;

        private const string AndroidClientId =
            "247683480444-n7gvs0lidng887pamkat36ds6mkot29f.apps.googleusercontent.com";

        private const string WindowsClientId =
            "247683480444-2lah8tu27r8tjjda7kktsonugp6vq1q2.apps.googleusercontent.com";

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public LoginViewModel(ApiService apiService, IReminderService reminderService)
        {
            _apiService = apiService;
            _reminderService = reminderService;
        }

        // ── E-Mail / Passwort Login ───────────────────────────────────────

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Bitte E-Mail und Passwort eingeben.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var request = new LoginRequest
                {
                    Email = Email,
                    Password = Password
                };

                var response = await _apiService.LoginAsync(request);

                if (response.Success)
                    await HandleSuccessfulLogin(response);
                else
                    ErrorMessage = $"Fehler: {response.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Google Login ──────────────────────────────────────────────────

        [RelayCommand]
        private async Task LoginWithGoogleAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
#if ANDROID
                var clientId = WindowsClientId; // Web Client ID für beide!
                var redirectUri = "http://localhost";
#else
                var clientId    = WindowsClientId;
                var redirectUri = "http://localhost:5287/api/auth/google-callback";
#endif
                var codeVerifier = GenerateCodeVerifier();
                var codeChallenge = GenerateCodeChallenge(codeVerifier);

                var authUrl = new Uri(
                    "https://accounts.google.com/o/oauth2/v2/auth" +
                    $"?client_id={Uri.EscapeDataString(clientId)}" +
                    "&response_type=code" +
                    "&scope=openid%20email%20profile" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    "&code_challenge_method=S256" +
                    $"&code_challenge={codeChallenge}");

                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = authUrl,
                        CallbackUrl = new Uri(redirectUri),
                        PrefersEphemeralWebBrowserSession = true
                    });

                if (result.Properties.TryGetValue("code", out var code)
                    && !string.IsNullOrEmpty(code))
                {
                    var response = await _apiService.GoogleExchangeAsync(code, redirectUri, codeVerifier);

                    if (response.Success)
                        await HandleSuccessfulLogin(response);
                    else
                        ErrorMessage = response.Message ?? "Google Login fehlgeschlagen.";
                }
                else
                {
                    ErrorMessage = "Kein Code von Google erhalten.";
                }
            }
            catch (TaskCanceledException)
            {
                // Nutzer hat Browser geschlossen — kein Fehler
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Navigation ────────────────────────────────────────────────────

        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────

        private async Task HandleSuccessfulLogin(AuthResponse response)
        {
            if (response.User?.ReminderEnabled == true)
            {
                await _reminderService.RequestPermissionAsync();
                _reminderService.StartPeriodicNotifications(
                    response.User.ReminderIntervalMinutes,
                    response.User.ReminderStartHour,
                    response.User.ReminderEndHour);
            }

            if (response.User?.SleepReminderEnabled == true)
            {
                _reminderService.ScheduleDailySleepReminder(
                    response.User.TargetBedTimeHour,
                    response.User.TargetBedTimeMinute);
            }

            await Shell.Current.GoToAsync("//WaterTracker");
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.ASCII.GetBytes(codeVerifier));
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static async Task<string?> ExchangeCodeForTokenAsync(
    string code,
    string redirectUri,
    string codeVerifier,
    string clientId)
        {
            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "code",          code                  },
            { "client_id",     clientId              },
            { "redirect_uri",  redirectUri           },
            { "grant_type",    "authorization_code"  },
            { "code_verifier", codeVerifier          }
        };

                var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent(parameters));

                var json = await response.Content.ReadAsStringAsync();

                // Debug — zeigt was Google zurückgibt
                System.Diagnostics.Debug.WriteLine($"Token Response: {json}");

                var tokenResponse = Newtonsoft.Json.JsonConvert
                    .DeserializeObject<Dictionary<string, string>>(json);

                if (tokenResponse != null &&
                    tokenResponse.TryGetValue("id_token", out var idToken))
                    return idToken;

                // Zeige Fehler aus Google Response
                if (tokenResponse != null &&
                    tokenResponse.TryGetValue("error", out var error))
                    System.Diagnostics.Debug.WriteLine($"Google Error: {error}");

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exchange Exception: {ex.Message}");
                return null;
            }
        }
    }
}