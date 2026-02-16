using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using MauiAbschlussprojekt.Services;
using System.Collections.ObjectModel;

namespace MauiAbschlussprojekt.ViewModels
{
    public partial class StatsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int averageMl;

        [ObservableProperty]
        private int currentStreak;

        [ObservableProperty]
        private int bestStreak;

        [ObservableProperty]
        private bool isLoading;

        public ObservableCollection<DailyStatsDto> WeekStats { get; } = new();

        public StatsViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task InitializeAsync()
        {
            await LoadStatsAsync();
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            IsLoading = true;

            try
            {
                var stats = await _apiService.GetWeekStatsAsync();

                if (stats != null)
                {
                    WeekStats.Clear();

                    // Umgekehrte Reihenfolge: neuester Tag zuerst
                    foreach (var day in stats.Days.OrderByDescending(d => d.Date))
                    {
                        WeekStats.Add(day);
                    }

                    AverageMl = stats.AverageMl;
                    CurrentStreak = stats.CurrentStreak;
                    BestStreak = stats.BestStreak;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fehler", $"Statistiken konnten nicht geladen werden: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}