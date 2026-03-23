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


            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<SleepApiService>();
            builder.Services.AddSingleton<IReminderService, ReminderService>();


            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<StatsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SleepTrackerPage>();
            builder.Services.AddTransient<SleepStatsPage>();
            builder.Services.AddTransient<AddSleepEntryPage>();


            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<StatsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SleepStatsViewModel>();
            builder.Services.AddTransient<AddSleepEntryViewModel>();


            builder.Services.AddSingleton<SleepTrackerViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
