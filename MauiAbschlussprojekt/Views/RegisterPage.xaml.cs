using MauiAbschlussprojekt.ViewModels;

namespace MauiAbschlussprojekt.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel vm)
    {
        InitializeComponent();
        this.BindingContext = vm;
    }
}