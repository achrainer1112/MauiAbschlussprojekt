using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiAbschlussprojekt.Services;
using Models;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IReminderService _reminderService;

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
                var clientId = WindowsClientId;
                var redirectUri = "http://localhost:5001/callback";

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

                // Listener VOR Browser starten
                var codeTask = StartLocalListenerAsync();

                await Browser.Default.OpenAsync(authUrl, BrowserLaunchMode.SystemPreferred);

                // Warten bis Code ankommt
                var code = await codeTask;

                if (!string.IsNullOrEmpty(code))
                {
                    var response = await _apiService.GoogleExchangeAsync(
                        code, redirectUri, codeVerifier);

                    if (response.Success)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
#if ANDROID
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

                            // Flag setzen — Navigation passiert wenn User Browser schließt
                            MauiAbschlussprojekt.MainActivity.ShouldNavigateToMain = true;
#else
                            await HandleSuccessfulLogin(response);
#endif
                        });
                    }
                    else
                    {
                        ErrorMessage = response.Message ?? "Google Login fehlgeschlagen.";
                    }
                }
                else
                {
                    ErrorMessage = "Kein Code von Google erhalten.";
                }
            }
            catch (TaskCanceledException)
            {
                // Nutzer hat abgebrochen
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

        // ── Lokaler HTTP Listener ─────────────────────────────────────────

        private static async Task<string?> StartLocalListenerAsync()
        {
            try
            {
                var listener = new System.Net.HttpListener();
                listener.Prefixes.Add("http://localhost:5001/callback/");
                listener.Start();

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

                var context = await Task.Run(
                    () => listener.GetContextAsync(), cts.Token);

                var url = context.Request.Url?.ToString() ?? "";

                var html = "<html><meta charset='utf-8'><body>" +
                           "<h2>Login erfolgreich! Kehre zur App zur\u00fcck.</h2>" +
                           "<script>window.close();</script>" +
                           "</body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.OutputStream.WriteAsync(buffer);
                context.Response.OutputStream.Close();
                listener.Stop();

                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var code = query["code"];

                System.Diagnostics.Debug.WriteLine($"Got code: {code}");
                return code;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Listener error: {ex.Message}");
                return null;
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
    }
}