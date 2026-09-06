using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
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

            _game = new EditorGame();
            _game.Run(GameContext);
        }

        protected override void OnDestroy()
        {
            _game?.Dispose();
            base.OnDestroy();
        }
    }
}
