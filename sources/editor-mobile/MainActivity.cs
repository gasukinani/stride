using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Stride.Engine;
using Stride.Games;
using Stride.Starter;

namespace StrideStudio.Mobile
{
    [Activity(
        Name = "com.gasukinani.stridestudio.MainActivity",
        Label = "Stride Mobile Studio",
        MainLauncher = true,
        Exported = true,
        Icon = "@android:drawable/sym_def_app_icon",
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    [IntentFilter(
        new[] { Intent.ActionMain },
        Categories = new[] { Intent.CategoryLauncher, Intent.CategoryDefault })]
    public class MainActivity : StrideActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                HideSystemUI();

                // KRITIKAL: Inilulunsad ang Stride Game gamit ang Android GameContext mula sa StrideActivity
                Game = new EditorGame();
                Game.Run(GameContext);
            }
            catch (System.Exception ex)
            {
                Log.Error("StrideStudio", $"Crash during startup: {ex}");
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                HideSystemUI();
            }
        }

        private void HideSystemUI()
        {
            if (Window == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                var controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
#pragma warning disable CS0618
                var uiOptions = (int)Window.DecorView.SystemUiVisibility;
                uiOptions |= (int)SystemUiFlags.LowProfile;
                uiOptions |= (int)SystemUiFlags.Fullscreen;
                uiOptions |= (int)SystemUiFlags.HideNavigation;
                uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
#pragma warning restore CS0618
            }
        }

        protected override void OnDestroy()
        {
            Game?.Dispose();
            base.OnDestroy();
        }
    }
}
