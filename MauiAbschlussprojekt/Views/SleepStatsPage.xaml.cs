using MauiAbschlussprojekt.ViewModels;

namespace MauiAbschlussprojekt.Views;

public partial class SleepStatsPage : ContentPage
{
    private readonly SleepStatsViewModel _viewModel;

    public SleepStatsPage(SleepStatsViewModel viewModel)
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
