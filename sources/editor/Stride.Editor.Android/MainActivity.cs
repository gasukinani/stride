using System;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Stride.Engine;
using Stride.Games;
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

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            ShowCrash(args.ExceptionObject?.ToString() ?? "Unknown exception");
        };

        try
        {
            _game = new Game();

            // Ipinapasa natin ang mismong Activity ('this') sa GameContext 
            // para makuha ni SDL ang tamang native window surface ng screen
            var context = GameContextFactory.NewGameContextAndroid(this);
            _game.Run(context);
        }
        catch (Exception ex)
        {
            ShowCrash(ex.ToString());
        }
    }

    private void ShowCrash(string message)
    {
        RunOnUiThread(() =>
        {
            var scroll = new ScrollView(this);
            var text = new TextView(this)
            {
                Text = "⚠️ STRIDE CRASH DETAILS:\n\n" + message,
                TextSize = 14
            };
            text.SetTextColor(Color.Red);
            text.SetBackgroundColor(Color.Black);
            text.SetPadding(30, 30, 30, 30);

            scroll.AddView(text);
            SetContentView(scroll);
        });
    }

    protected override void OnDestroy()
    {
        _game?.Dispose();
        base.OnDestroy();
    }
}
