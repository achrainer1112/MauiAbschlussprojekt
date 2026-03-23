using Android.App;
using Android.Content.PM;

namespace MauiAbschlussprojekt.Platforms.Android
{
    [Activity(
        NoHistory = true,
        LaunchMode = LaunchMode.SingleTop,
        Exported = true)]
    [IntentFilter(
        new[] { global::Android.Content.Intent.ActionView },
        Categories = new[]
        {
            global::Android.Content.Intent.CategoryDefault,
            global::Android.Content.Intent.CategoryBrowsable
        },
        DataScheme = "http",
        DataHost = "localhost")]
    public class WebAuthenticatorCallbackActivity
        : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
    }
}