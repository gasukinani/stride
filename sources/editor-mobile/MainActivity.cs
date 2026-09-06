using Android.App;
using Android.Content.PM;
using Android.OS;
using Stride.Engine;
using Stride.Starter;

namespace StrideStudio.Mobile
{
    [Activity(
        Label = "Stride Mobile Studio",
        MainLauncher = true,
        Icon = "@android:drawable/sym_def_app_icon",
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        private EditorGame? _game;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Inilulunsad ang Stride Engine gamit ang Game Context ng Android
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
