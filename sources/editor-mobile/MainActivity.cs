using Android.App;
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
    public class MainActivity : AndroidGameActivity
    {
        private EditorGame? _game;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Inilulunsad ang Stride Engine
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
