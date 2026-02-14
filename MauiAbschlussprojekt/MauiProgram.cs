using CommunityToolkit.Maui;
using MauiAbschlussprojekt.Services;
using MauiAbschlussprojekt.ViewModels;
using MauiAbschlussprojekt.Views;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace MauiAbschlussprojekt
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
#if ANDROID || IOS
                .UseLocalNotification()
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<IReminderService, ReminderService>();

            // Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StatsPage>();
            builder.Services.AddTransient<SettingsPage>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<StatsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}