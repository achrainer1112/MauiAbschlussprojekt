using MauiAbschlussprojekt.ViewModels;

namespace MauiAbschlussprojekt.Views
{
    public partial class SleepTrackerPage : ContentPage
    {
        private readonly SleepTrackerViewModel _viewModel;

        public SleepTrackerPage(SleepTrackerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}