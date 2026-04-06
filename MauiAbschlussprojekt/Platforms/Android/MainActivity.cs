using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace MauiAbschlussprojekt
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity? Instance { get; private set; }
        public static bool ShouldNavigateToMain { get; set; } = false;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
        }

        protected override void OnResume()
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine($"OnResume called! ShouldNavigate={ShouldNavigateToMain}");

            if (ShouldNavigateToMain)
            {
                ShouldNavigateToMain = false;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(300);
                    System.Diagnostics.Debug.WriteLine("Navigating to WaterTracker...");
                    await Shell.Current.GoToAsync("//WaterTracker");
                });
            }
        }
    }
}