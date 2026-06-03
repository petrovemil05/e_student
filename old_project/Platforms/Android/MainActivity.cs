using Android.App;
using Android.Content.PM;
using Android.OS;

namespace e_student
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                RequestPermissions(
                    new[] { "android.permission.POST_NOTIFICATIONS" },
                    requestCode: 0);
            }
        }
    }

}
