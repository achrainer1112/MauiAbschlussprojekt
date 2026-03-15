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

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public LoginViewModel(ApiService apiService, IReminderService reminderService) // neu
        {
            _apiService = apiService;
            _reminderService = reminderService; // neu
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
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
                {
                    // Reminder beim Login wiederherstellen
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
                else
                {
                    ErrorMessage = $"Fehler: {response.Message}";
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
        private async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}