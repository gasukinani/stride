using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Widget;
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
    private bool _isStarted = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Sinasalo ang mga crash
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            ShowCrash(args.ExceptionObject?.ToString() ?? "Unknown exception");
        };
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        // Hihintayin muna nating magkaroon ng focus ang screen bago simulan ang Stride Engine
        if (hasFocus && !_isStarted)
        {
            _isStarted = true;
            StartGameEngine();
        }
    }

    private void StartGameEngine()
    {
        Task.Run(async () =>
        {
            // Bigyan ng 300ms ang Android OS para mai-attach ang Native Window sa SDL
            await Task.Delay(300);

            RunOnUiThread(() =>
            {
                try
                {
                    _game = new Game();
                    _game.Run(); // Walang arguments para automatic ang Android context
                }
                catch (Exception ex)
                {
                    ShowCrash(ex.ToString());
                }
            });
        });
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
