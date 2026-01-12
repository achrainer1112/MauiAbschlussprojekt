using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private int totalMl;

        [ObservableProperty]
        private int goalMl = 2000;

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressText = "0%";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string customAmountInput = string.Empty;

        public ObservableCollection<WaterEntryDto> TodayEntries { get; } = new();

        public MainViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task InitializeAsync()
        {
            Username = _apiService.CurrentUser?.Username ?? "User";
            GoalMl = _apiService.CurrentUser?.DailyWaterGoalMl ?? 2000;
            await LoadTodayDataAsync();
        }

        [RelayCommand]
        private async Task LoadTodayDataAsync()
        {
            IsLoading = true;

            try
            {
                var entries = await _apiService.GetTodayEntriesAsync();

                TodayEntries.Clear();
                foreach (var entry in entries)
                {
                    TodayEntries.Add(entry);
                }

                TotalMl = entries.Sum(e => e.AmountMl);
                UpdateProgress();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Daten konnten nicht geladen werden: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task QuickAddWaterAsync(int amountMl)
        {
            try
            {
                var request = new AddWaterRequest
                {
                    AmountMl = amountMl,
                    LoggedAt = DateTime.Now
                };

                var newEntry = await _apiService.AddWaterAsync(request);

                if (newEntry != null)
                {
                    TodayEntries.Insert(0, newEntry);
                    TotalMl += amountMl;
                    UpdateProgress();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Hinzufügen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AddCustomAmountAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomAmountInput))
                return;

            if (!int.TryParse(CustomAmountInput, out int amount) || amount <= 0)
            {
                await Shell.Current.DisplayAlert("Fehler", "Bitte eine gültige Menge eingeben", "OK");
                return;
            }

            await QuickAddWaterAsync(amount);
            CustomAmountInput = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteEntryAsync(WaterEntryDto entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Löschen",
                $"Eintrag ({entry.AmountMl}ml) wirklich löschen?",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            try
            {
                bool success = await _apiService.DeleteWaterAsync(entry.Id);

                if (success)
                {
                    TodayEntries.Remove(entry);
                    TotalMl -= entry.AmountMl;
                    UpdateProgress();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Löschen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EditEntryAsync(WaterEntryDto entry)
        {
            string result = await Shell.Current.DisplayPromptAsync(
                "Bearbeiten",
                "Neue Menge (ml):",
                initialValue: entry.AmountMl.ToString(),
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (!int.TryParse(result, out int newAmount) || newAmount <= 0)
            {
                await Shell.Current.DisplayAlert("Fehler", "Ungültige Menge", "OK");
                return;
            }

            try
            {
                var request = new UpdateWaterEntryRequest
                {
                    Id = entry.Id,
                    AmountMl = newAmount,
                    LoggedAt = entry.LoggedAt
                };

                var updated = await _apiService.UpdateWaterAsync(request);

                if (updated != null)
                {
                    int oldAmount = entry.AmountMl;
                    entry.AmountMl = newAmount;
                    TotalMl = TotalMl - oldAmount + newAmount;
                    UpdateProgress();

                    // UI-Update erzwingen
                    var index = TodayEntries.IndexOf(entry);
                    if (index >= 0)
                    {
                        TodayEntries[index] = updated;
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Fehler beim Bearbeiten: {ex.Message}", "OK");
            }
        }

        private void UpdateProgress()
        {
            ProgressPercentage = GoalMl > 0 ? Math.Min((double)TotalMl / GoalMl, 1.0) : 0;
            int percentage = (int)(ProgressPercentage * 100);
            ProgressText = $"{percentage}%";
        }

        [RelayCommand]
        private async Task NavigateToStatsAsync()
        {
            await Shell.Current.GoToAsync("//StatsPage");
        }

        [RelayCommand]
        private async Task NavigateToSettingsAsync()
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            _apiService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}