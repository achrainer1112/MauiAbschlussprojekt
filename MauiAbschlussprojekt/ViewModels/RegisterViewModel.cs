using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string weightInput = string.Empty;

        [ObservableProperty]
        private ActivityLevelItem selectedActivityLevel;

        [ObservableProperty]
        private string customGoalInput = string.Empty;

        [ObservableProperty]
        private string calculatedGoalText = string.Empty;

        [ObservableProperty]
        private bool showCalculatedGoal;

        [ObservableProperty]
        private bool showCustomGoal;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public ObservableCollection<ActivityLevelItem> ActivityLevels { get; }

        public RegisterViewModel(ApiService apiService)
        {
            _apiService = apiService;

            ActivityLevels = new ObservableCollection<ActivityLevelItem>
            {
                new ActivityLevelItem { Value = "low", Display = "Niedrig (Bürojob, wenig Bewegung)" },
                new ActivityLevelItem { Value = "medium", Display = "Mittel (Normale Aktivität)" },
                new ActivityLevelItem { Value = "high", Display = "Hoch (Sport, körperliche Arbeit)" }
            };

            selectedActivityLevel = ActivityLevels[1]; // Default: medium
        }

        partial void OnWeightInputChanged(string value)
        {
            CalculateWaterGoal();
        }

        partial void OnSelectedActivityLevelChanged(ActivityLevelItem value)
        {
            CalculateWaterGoal();
        }

        private void CalculateWaterGoal()
        {
            if (double.TryParse(WeightInput, out double weight) && weight > 0)
            {
                double multiplier = SelectedActivityLevel.Value switch
                {
                    "low" => 30,
                    "high" => 40,
                    _ => 35
                };
                int goal = (int)(weight * multiplier);
                CalculatedGoalText = $"Empfohlenes Tagesziel: {goal} ml";
                ShowCalculatedGoal = true;
            }
            else
            {
                ShowCalculatedGoal = false;
            }
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Bitte Username, Email und Passwort ausfüllen";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwörter stimmen nicht überein";
                return;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein";
                return;
            }

            IsLoading = true;

            try
            {
                var request = new RegisterRequest
                {
                    Username = Username,
                    Email = Email,
                    Password = Password
                };


                if (double.TryParse(WeightInput, out double weight) && weight > 0)
                {
                    request.WeightKg = weight;
                    request.ActivityLevel = SelectedActivityLevel.Value;
                }


                if (ShowCustomGoal && int.TryParse(CustomGoalInput, out int customGoal) && customGoal > 0)
                {
                    request.CustomDailyGoalMl = customGoal;
                }

                var response = await _apiService.RegisterAsync(request);

                if (response.Success)
                {
                    await Shell.Current.DisplayAlert("Erfolg", "Registrierung erfolgreich!", "OK");
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    ErrorMessage = response.Message;
                }
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

        [RelayCommand]
        private async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public class ActivityLevelItem
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }
}
