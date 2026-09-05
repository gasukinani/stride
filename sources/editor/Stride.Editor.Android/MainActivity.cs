using Android.App;
using Android.Content.PM;
using Android.OS;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Editor.Android;

[Activity(
    Label = "Stride Engine Android",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
    ScreenOrientation = ScreenOrientation.Landscape)] // Mas maganda ang landscape para sa laro at editor
public class MainActivity : AndroidStrideActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Sinisimulan nito ang mismong Stride Engine Game Context
        Game = new Game();
        Run(Game);
    }
}
