using MauiAbschlussprojekt.Services;
using MauiAbschlussprojekt.Views;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;

namespace MauiAbschlussprojekt
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("StatsPage", typeof(StatsPage));
            Routing.RegisterRoute("SleepStatsPage", typeof(SleepStatsPage));
            Routing.RegisterRoute("AddSleepEntryPage", typeof(AddSleepEntryPage));

            FlyoutBehavior = FlyoutBehavior.Disabled;

            var style = new Microsoft.Maui.Controls.Style(typeof(Grid));
            var vsg = new VisualStateGroupList();
            var vg = new VisualStateGroup { Name = "CommonStates" };

            var normal = new VisualState { Name = "Normal" };
            normal.Setters.Add(new Setter
            {
                Property = BackgroundColorProperty,
                Value = Colors.Transparent
            });

            var selected = new VisualState { Name = "Selected" };
            selected.Setters.Add(new Setter
            {
                Property = BackgroundColorProperty,
                Value = Color.FromArgb("#2A3A4A")
            });

            vg.States.Add(normal);
            vg.States.Add(selected);
            vsg.Add(vg);
            style.Setters.Add(new Setter
            {
                Property = VisualStateManager.VisualStateGroupsProperty,
                Value = vsg
            });

            Navigated += OnNavigated;
        }

        private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            var currentRoute = e.Current?.Location?.OriginalString ?? "";
            bool isAuthPage = currentRoute.Contains("LoginPage") || currentRoute.Contains("RegisterPage");
            FlyoutBehavior = isAuthPage ? FlyoutBehavior.Disabled : FlyoutBehavior.Flyout;

            if (!isAuthPage)
                UpdateFlyoutHeader();
        }

        public void UpdateFlyoutHeader()
        {
            var apiService = Handler?.MauiContext?.Services.GetService<ApiService>();
            if (apiService?.CurrentUser == null)
                return;

            var username = apiService.CurrentUser.Username ?? "User";

            if (FlyoutUsernameLabel != null)
                FlyoutUsernameLabel.Text = username;

            if (AvatarLabel != null)
            {
                AvatarLabel.Text = username.Length >= 2
                    ? username.Substring(0, 2).ToUpper()
                    : username.ToUpper();
            }

            if (AvatarFrame != null)
                AvatarFrame.BackgroundColor = GetAvatarColor(username);
        }

        private static Color GetAvatarColor(string username)
        {
            var colors = new[]
            {
                Color.FromArgb("#1A73E8"),
                Color.FromArgb("#1565C0"),
                Color.FromArgb("#0277BD"),
                Color.FromArgb("#1A73E8"),
                Color.FromArgb("#283593"),
                Color.FromArgb("#0288D1"),
                Color.FromArgb("#1A73E8"),
                Color.FromArgb("#1976D2"),
            };

            int index = username.Length > 0
                ? Math.Abs(username[0]) % colors.Length
                : 0;

            return colors[index];
        }
    }
}
