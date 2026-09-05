using System;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Widget;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Editor.Android;

[Activity(
    Label = "Stride Editor",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class MainActivity : StrideActivity
{
    private Game? _game;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Saluhin ang mga hindi inaasahang crash sa C# domain
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject?.ToString() ?? "Unknown exception";
            Log.Error("StrideCrash", ex);
            ShowCrash(ex);
        };

        base.OnCreate(savedInstanceState);

        try
        {
            // 1. Gumawa ng instance ng Stride Game
            _game = new Game();

            // 2. IMPORTANT: Ipasa ang GameContext mula sa StrideActivity
            // Ang StrideActivity na ang bahalang mag-manage ng render loop
            _game.Run(GameContext);
        }
        catch (Exception ex)
        {
            Log.Error("StrideCrash", ex.ToString());
            ShowCrash(ex.ToString());
        }
    }

    private void ShowCrash(string message)
    {
        RunOnUiThread(() =>
        {
            try
            {
                var scroll = new ScrollView(this);
                var text = new TextView(this)
                {
                    Text = "⚠️ STRIDE CRASH DETAILS:\n\n" + message,
                    TextSize = 13
                };
                text.SetTextColor(Color.Red);
                text.SetBackgroundColor(Color.Black);
                text.SetPadding(40, 40, 40, 40);

                scroll.AddView(text);
                SetContentView(scroll);
            }
            catch
            {
                // Fallback kung sira na ang activity state
            }
        });
    }

    protected override void OnDestroy()
    {
        try
        {
            _game?.Dispose();
            _game = null;
        }
        catch (Exception ex)
        {
            Log.Warn("StrideEditor", $"Error during game dispose: {ex.Message}");
        }

        base.OnDestroy();
    }
}
