using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Stride.Engine;
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
    public class MainActivity : AndroidGameActivity
    {
        private EditorGame? _game;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Tiyaking Fullscreen at nakatago ang System Navigation Bar para sa Game Engine UI
            HideSystemUI();

            // Simulan ang Stride Game Context
            _game = new EditorGame();
            _game.Run(GameContext);
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

            // Immersive Sticky Fullscreen mode para sa Android tablets at phones
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
#pragma warning disable CS0618 // Type or member is obsolete
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
            _game?.Dispose();
            base.OnDestroy();
        }
    }
}
