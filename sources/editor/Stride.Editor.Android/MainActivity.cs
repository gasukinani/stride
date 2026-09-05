using System;
using System.Threading.Tasks;
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
    private bool _isEngineStarted = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Tagasalo ng unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject?.ToString() ?? "Unknown exception";
            Log.Error("StrideCrash", ex);
            ShowCrash(ex);
        };

        base.OnCreate(savedInstanceState);
        // Hayaan munang matapos ang OnCreate nang HINDI tinatawag ang _game.Run dito
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        // Sisimulan lamang kapag nakalabas na sa screen ang window at may focus na
        if (hasFocus && !_isEngineStarted)
        {
            _isEngineStarted = true;
            StartGameEngineAsync();
        }
    }

    private void StartGameEngineAsync()
    {
        // KRITIKAL: Patakbuhin sa BACKGROUND THREAD gamit ang Task.Run
        // HUWAG gagamit ng RunOnUiThread dito dahil iba-block nito ang Android Looper
        Task.Run(async () =>
        {
            const int maxRetries = 10;
            int attempts = 0;

            while (attempts < maxRetries)
            {
                try
                {
                    // Bigyan ng 200-400ms ang Android OS para mai-bind ng SDL SurfaceView ang ANativeWindow
                    await Task.Delay(200 + (attempts * 150));

                    _game = new Game();

                    // Ito ay magsisilbing blocking render loop sa background thread
                    _game.Run();

                    // Kung maayos na nag-exit ang game loop
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("native window", StringComparison.OrdinalIgnoreCase) && attempts < maxRetries - 1)
                {
                    attempts++;
                    Log.Warn("StrideEditor", $"Naglalaan pa ang Android ng Native Window... Pagsubok muli ({attempts}/{maxRetries})");

                    try
                    {
                        _game?.Dispose();
                    }
                    catch { }
                    _game = null;
                }
                catch (Exception ex)
                {
                    Log.Error("StrideCrash", ex.ToString());
                    ShowCrash(ex.ToString());
                    break;
                }
            }
        });
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
                // Fallback kapag sira na ang activity state
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
            Log.Warn("StrideEditor", $"Error habang nagdi-dispose: {ex.Message}");
        }

        base.OnDestroy();
    }
}
