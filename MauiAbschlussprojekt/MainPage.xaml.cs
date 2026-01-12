using MauiAbschlussprojekt.Services;

namespace MauiAbschlussprojekt
{
    public partial class MainPage : ContentPage
    {
        private readonly ApiService _apiService;

        public MainPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            _apiService.Logout();
            await Shell.Current.GoToAsync("///LoginPage");
        }
    }
}