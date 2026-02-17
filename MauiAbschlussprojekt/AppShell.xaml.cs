using MauiAbschlussprojekt.Views;

namespace MauiAbschlussprojekt
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
            Routing.RegisterRoute("StatsPage", typeof(StatsPage));
            Routing.RegisterRoute("SleepStatsPage", typeof(SleepStatsPage));
            Routing.RegisterRoute("AddSleepEntryPage", typeof(AddSleepEntryPage));
        }
    }
}