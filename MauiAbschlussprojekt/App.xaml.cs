using Microsoft.Extensions.DependencyInjection;

namespace MauiAbschlussprojekt
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

            // Nach dem Laden zur LoginPage 
            shell.Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync("//LoginPage");
            });

            return window;
        }
    }
}