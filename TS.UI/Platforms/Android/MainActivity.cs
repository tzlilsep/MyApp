using Android.App;
using Android.Content;        // ← נדרש ל-[IntentFilter]
using Android.Content.PM;
using Android.OS;

namespace TS.UI
{
    // לא נוגעים בהגדרת ה-Activity הקיימת
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]

    // 🔽 קולט חזרה מה-Hosted UI (login)
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "myapp", DataHost = "auth", DataPathPrefix = "/callback",
        AutoVerify = true)]

    // 🔽 קולט sign-out
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "myapp", DataHost = "auth", DataPathPrefix = "/signout")]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}

