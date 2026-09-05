using Android.App;
using Android.Content.PM;
using Android.OS;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Editor.Android;

[Activity(
    Label = "Stride Editor",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class MainActivity : StrideActivity
{
    private Game? _game;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Tamang syntax: lowercase variable at game.Run()
        _game = new Game();
        _game.Run();
    }

    protected override void OnDestroy()
    {
        _game?.Dispose();
        base.OnDestroy();
    }
}
