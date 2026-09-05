using System;
using System.Threading.Tasks;
using AndroidApp = global::Android.App;
using AndroidContent = global::Android.Content;
using AndroidGraphics = global::Android.Graphics;
using AndroidOS = global::Android.OS;
using AndroidText = global::Android.Text;
using AndroidUtil = global::Android.Util;
using AndroidViews = global::Android.Views;
using AndroidWidgets = global::Android.Widget;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Editor.Android;

[AndroidApp.Activity(
    Label = "Stride Editor",
    MainLauncher = true,
    ConfigurationChanges = AndroidContent.PM.ConfigChanges.Orientation | AndroidContent.PM.ConfigChanges.ScreenSize | AndroidContent.PM.ConfigChanges.KeyboardHidden | AndroidContent.PM.ConfigChanges.ScreenLayout,
    ScreenOrientation = AndroidContent.PM.ScreenOrientation.Landscape)]
public class MainActivity : StrideActivity
{
    private EditorGame? _editor;
    private bool _engineInitialized = false;

    // UI Controls para sa Editor
    private AndroidWidgets.LinearLayout? _hierarchyPanel;
    private AndroidWidgets.TextView? _inspectorTitle;
    private AndroidWidgets.EditText? _posXInput, _posYInput, _posZInput;

    protected override void OnCreate(AndroidOS.Bundle? savedInstanceState)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            AndroidUtil.Log.Error("StrideEditorCrash", args.ExceptionObject?.ToString() ?? "Unknown exception");
        };

        base.OnCreate(savedInstanceState);
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus && !_engineInitialized)
        {
            _engineInitialized = true;
            InitializeEditorAsync();
        }
    }

    private void InitializeEditorAsync()
    {
        Task.Run(async () =>
        {
            const int maxRetries = 12;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    await Task.Delay(250 + (attempt * 150));

                    _editor = new EditorGame();

                    _editor.OnHierarchyChanged += (entities) =>
                    {
                        RunOnUiThread(() => RefreshHierarchyUI(entities));
                    };

                    _editor.OnEntitySelected += (entity) =>
                    {
                        RunOnUiThread(() => RefreshInspectorUI(entity));
                    };

                    RunOnUiThread(() => BuildEditorOverlay());

                    _editor.Run();
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("native window", StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
                {
                    attempt++;
                    AndroidUtil.Log.Warn("StrideEditor", $"Inihahanda ang Surface... Retry {attempt}/{maxRetries}");
                    _editor?.Dispose();
                    _editor = null;
                }
                catch (Exception ex)
                {
                    AndroidUtil.Log.Error("StrideEditorCrash", ex.ToString());
                    RunOnUiThread(() => ShowCrashDialog(ex.ToString()));
                    break;
                }
            }
        });
    }

    private void BuildEditorOverlay()
    {
        var rootLayout = new AndroidWidgets.RelativeLayout(this)
        {
            LayoutParameters = new AndroidWidgets.RelativeLayout.LayoutParams(
                AndroidViews.ViewGroup.LayoutParams.MatchParent,
                AndroidViews.ViewGroup.LayoutParams.MatchParent)
        };

        // 1. TOP TOOLBAR
        var toolbar = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Horizontal
        };
        toolbar.SetBackgroundColor(AndroidGraphics.Color.Argb(220, 25, 25, 25));
        toolbar.SetPadding(20, 10, 20, 10);

        var btnAddCube = CreateButton("+ Cube");
        btnAddCube.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Cube, "Cube_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        var btnAddSphere = CreateButton("+ Sphere");
        btnAddSphere.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Sphere, "Sphere_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        toolbar.AddView(btnAddCube);
        toolbar.AddView(btnAddSphere);

        var toolbarParams = new AndroidWidgets.RelativeLayout.LayoutParams(AndroidViews.ViewGroup.LayoutParams.MatchParent, AndroidViews.ViewGroup.LayoutParams.WrapContent);
        toolbarParams.AddRule(AndroidWidgets.LayoutRules.AlignParentTop);
        rootLayout.AddView(toolbar, toolbarParams);

        // 2. LEFT PANEL: SCENE HIERARCHY
        _hierarchyPanel = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Vertical
        };
        _hierarchyPanel.SetBackgroundColor(AndroidGraphics.Color.Argb(200, 35, 35, 35));
        _hierarchyPanel.SetPadding(20, 20, 20, 20);

        var hierarchyTitle = new AndroidWidgets.TextView(this) { Text = "📂 SCENE HIERARCHY", TextSize = 14 };
        hierarchyTitle.SetTextColor(AndroidGraphics.Color.Yellow);
        _hierarchyPanel.AddView(hierarchyTitle);

        var hierarchyScroll = new AndroidWidgets.ScrollView(this);
        hierarchyScroll.AddView(_hierarchyPanel);

        var hierarchyParams = new AndroidWidgets.RelativeLayout.LayoutParams(400, AndroidViews.ViewGroup.LayoutParams.MatchParent);
        hierarchyParams.AddRule(AndroidWidgets.LayoutRules.AlignParentLeft);
        hierarchyParams.TopMargin = 100;
        rootLayout.AddView(hierarchyScroll, hierarchyParams);

        // 3. RIGHT PANEL: INSPECTOR
        var inspectorLayout = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Vertical
        };
        inspectorLayout.SetBackgroundColor(AndroidGraphics.Color.Argb(200, 35, 35, 35));
        inspectorLayout.SetPadding(20, 20, 20, 20);

        _inspectorTitle = new AndroidWidgets.TextView(this) { Text = "⚙️ INSPECTOR: (No Selection)", TextSize = 14 };
        _inspectorTitle.SetTextColor(AndroidGraphics.Color.Cyan);
        inspectorLayout.AddView(_inspectorTitle);

        var posLabel = new AndroidWidgets.TextView(this) { Text = "Position (X, Y, Z):" };
        posLabel.SetTextColor(AndroidGraphics.Color.White);
        inspectorLayout.AddView(posLabel);

        _posXInput = CreateNumberInput("0");
        _posYInput = CreateNumberInput("0");
        _posZInput = CreateNumberInput("0");

        var applyBtn = CreateButton("Apply Transform");
        applyBtn.Click += (s, e) =>
        {
            if (_posXInput?.Text != null && _posYInput?.Text != null && _posZInput?.Text != null &&
                float.TryParse(_posXInput.Text, out float x) &&
                float.TryParse(_posYInput.Text, out float y) &&
                float.TryParse(_posZInput.Text, out float z))
            {
                _editor?.UpdateEntityPosition(new Vector3(x, y, z));
            }
        };

        inspectorLayout.AddView(_posXInput);
        inspectorLayout.AddView(_posYInput);
        inspectorLayout.AddView(_posZInput);
        inspectorLayout.AddView(applyBtn);

        var inspectorParams = new AndroidWidgets.RelativeLayout.LayoutParams(420, AndroidViews.ViewGroup.LayoutParams.MatchParent);
        inspectorParams.AddRule(AndroidWidgets.LayoutRules.AlignParentRight);
        inspectorParams.TopMargin = 100;
        rootLayout.AddView(inspectorLayout, inspectorParams);

        AddContentView(rootLayout, new AndroidViews.ViewGroup.LayoutParams(AndroidViews.ViewGroup.LayoutParams.MatchParent, AndroidViews.ViewGroup.LayoutParams.MatchParent));
    }

    private void RefreshHierarchyUI(System.Collections.Generic.List<Entity> entities)
    {
        if (_hierarchyPanel == null) return;

        while (_hierarchyPanel.ChildCount > 1)
        {
            _hierarchyPanel.RemoveViewAt(1);
        }

        foreach (var entity in entities)
        {
            var itemBtn = new AndroidWidgets.Button(this)
            {
                Text = "📦 " + entity.Name,
                TextSize = 12
            };
            itemBtn.SetBackgroundColor(AndroidGraphics.Color.Argb(180, 50, 50, 50));
            itemBtn.SetTextColor(AndroidGraphics.Color.White);
            itemBtn.Click += (s, e) => _editor?.SelectEntity(entity);
            _hierarchyPanel.AddView(itemBtn);
        }
    }

    private void RefreshInspectorUI(Entity? entity)
    {
        if (entity == null || _inspectorTitle == null) return;

        _inspectorTitle.Text = "⚙️ " + entity.Name;
        if (_posXInput != null) _posXInput.Text = entity.Transform.Position.X.ToString("F2");
        if (_posYInput != null) _posYInput.Text = entity.Transform.Position.Y.ToString("F2");
        if (_posZInput != null) _posZInput.Text = entity.Transform.Position.Z.ToString("F2");
    }

    private AndroidWidgets.Button CreateButton(string text)
    {
        var btn = new AndroidWidgets.Button(this) { Text = text };
        btn.SetTextColor(AndroidGraphics.Color.White);
        btn.SetBackgroundColor(AndroidGraphics.Color.Argb(255, 60, 60, 60));
        return btn;
    }

    private AndroidWidgets.EditText CreateNumberInput(string val)
    {
        var edit = new AndroidWidgets.EditText(this) { Text = val };
        edit.SetTextColor(AndroidGraphics.Color.White);
        edit.SetBackgroundColor(AndroidGraphics.Color.Argb(150, 20, 20, 20));
        edit.InputType = AndroidText.InputTypes.ClassNumber | AndroidText.InputTypes.NumberFlagDecimal | AndroidText.InputTypes.NumberFlagSigned;
        return edit;
    }

    private void ShowCrashDialog(string error)
    {
        new AndroidApp.AlertDialog.Builder(this)
            .SetTitle("Editor Exception")
            .SetMessage(error)
            .SetPositiveButton("OK", (s, e) => { })
            .Show();
    }

    protected override void OnDestroy()
    {
        _editor?.Dispose();
        _editor = null;
        base.OnDestroy();
    }
}                    {
                        RunOnUiThread(() => RefreshHierarchyUI(entities));
                    };

                    _editor.OnEntitySelected += (entity) =>
                    {
                        RunOnUiThread(() => RefreshInspectorUI(entity));
                    };

                    RunOnUiThread(() => BuildEditorOverlay());

                    _editor.Run();
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("native window", StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
                {
                    attempt++;
                    AndroidUtil.Log.Warn("StrideEditor", $"Inihahanda ang Surface... Retry {attempt}/{maxRetries}");
                    _editor?.Dispose();
                    _editor = null;
                }
                catch (Exception ex)
                {
                    AndroidUtil.Log.Error("StrideEditorCrash", ex.ToString());
                    RunOnUiThread(() => ShowCrashDialog(ex.ToString()));
                    break;
                }
            }
        });
    }

    // ==========================================
    // STRIDE EDITOR INTERFACE (Android UI OVERLAY)
    // ==========================================

    private void BuildEditorOverlay()
    {
        var rootLayout = new AndroidWidgets.RelativeLayout(this)
        {
            LayoutParameters = new AndroidWidgets.RelativeLayout.LayoutParams(
                AndroidViews.ViewGroup.LayoutParams.MatchParent,
                AndroidViews.ViewGroup.LayoutParams.MatchParent)
        };

        // 1. TOP TOOLBAR
        var toolbar = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Horizontal
        };
        toolbar.SetBackgroundColor(AndroidGraphics.Color.Argb(220, 25, 25, 25));
        toolbar.SetPadding(20, 10, 20, 10);

        var btnAddCube = CreateButton("+ Cube");
        btnAddCube.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Cube, "Cube_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        var btnAddSphere = CreateButton("+ Sphere");
        btnAddSphere.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Sphere, "Sphere_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        toolbar.AddView(btnAddCube);
        toolbar.AddView(btnAddSphere);

        var toolbarParams = new AndroidWidgets.RelativeLayout.LayoutParams(AndroidViews.ViewGroup.LayoutParams.MatchParent, AndroidViews.ViewGroup.LayoutParams.WrapContent);
        toolbarParams.AddRule(AndroidWidgets.LayoutRules.AlignParentTop);
        rootLayout.AddView(toolbar, toolbarParams);

        // 2. LEFT PANEL: SCENE HIERARCHY
        _hierarchyPanel = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Vertical
        };
        _hierarchyPanel.SetBackgroundColor(AndroidGraphics.Color.Argb(200, 35, 35, 35));
        _hierarchyPanel.SetPadding(20, 20, 20, 20);

        var hierarchyTitle = new AndroidWidgets.TextView(this) { Text = "📂 SCENE HIERARCHY", TextSize = 14 };
        hierarchyTitle.SetTextColor(AndroidGraphics.Color.Yellow);
        _hierarchyPanel.AddView(hierarchyTitle);

        var hierarchyScroll = new AndroidWidgets.ScrollView(this);
        hierarchyScroll.AddView(_hierarchyPanel);

        var hierarchyParams = new AndroidWidgets.RelativeLayout.LayoutParams(400, AndroidViews.ViewGroup.LayoutParams.MatchParent);
        hierarchyParams.AddRule(AndroidWidgets.LayoutRules.AlignParentLeft);
        hierarchyParams.TopMargin = 100;
        rootLayout.AddView(hierarchyScroll, hierarchyParams);

        // 3. RIGHT PANEL: INSPECTOR
        var inspectorLayout = new AndroidWidgets.LinearLayout(this)
        {
            Orientation = AndroidWidgets.Orientation.Vertical
        };
        inspectorLayout.SetBackgroundColor(AndroidGraphics.Color.Argb(200, 35, 35, 35));
        inspectorLayout.SetPadding(20, 20, 20, 20);

        _inspectorTitle = new AndroidWidgets.TextView(this) { Text = "⚙️ INSPECTOR: (No Selection)", TextSize = 14 };
        _inspectorTitle.SetTextColor(AndroidGraphics.Color.Cyan);
        inspectorLayout.AddView(_inspectorTitle);

        var posLabel = new AndroidWidgets.TextView(this) { Text = "Position (X, Y, Z):" };
        posLabel.SetTextColor(AndroidGraphics.Color.White);
        inspectorLayout.AddView(posLabel);

        _posXInput = CreateNumberInput("0");
        _posYInput = CreateNumberInput("0");
        _posZInput = CreateNumberInput("0");

        var applyBtn = CreateButton("Apply Transform");
        applyBtn.Click += (s, e) =>
        {
            if (_posXInput?.Text != null && _posYInput?.Text != null && _posZInput?.Text != null &&
                float.TryParse(_posXInput.Text, out float x) &&
                float.TryParse(_posYInput.Text, out float y) &&
                float.TryParse(_posZInput.Text, out float z))
            {
                _editor?.UpdateEntityPosition(new Vector3(x, y, z));
            }
        };

        inspectorLayout.AddView(_posXInput);
        inspectorLayout.AddView(_posYInput);
        inspectorLayout.AddView(_posZInput);
        inspectorLayout.AddView(applyBtn);

        var inspectorParams = new AndroidWidgets.RelativeLayout.LayoutParams(420, AndroidViews.ViewGroup.LayoutParams.MatchParent);
        inspectorParams.AddRule(AndroidWidgets.LayoutRules.AlignParentRight);
        inspectorParams.TopMargin = 100;
        rootLayout.AddView(inspectorLayout, inspectorParams);

        AddContentView(rootLayout, new AndroidViews.ViewGroup.LayoutParams(AndroidViews.ViewGroup.LayoutParams.MatchParent, AndroidViews.ViewGroup.LayoutParams.MatchParent));
    }

    private void RefreshHierarchyUI(System.Collections.Generic.List<Entity> entities)
    {
        if (_hierarchyPanel == null) return;

        while (_hierarchyPanel.ChildCount > 1)
        {
            _hierarchyPanel.RemoveViewAt(1);
        }

        foreach (var entity in entities)
        {
            var itemBtn = new AndroidWidgets.Button(this)
            {
                Text = "📦 " + entity.Name,
                TextSize = 12
            };
            itemBtn.SetBackgroundColor(AndroidGraphics.Color.Argb(180, 50, 50, 50));
            itemBtn.SetTextColor(AndroidGraphics.Color.White);
            itemBtn.Click += (s, e) => _editor?.SelectEntity(entity);
            _hierarchyPanel.AddView(itemBtn);
        }
    }

    private void RefreshInspectorUI(Entity? entity)
    {
        if (entity == null || _inspectorTitle == null) return;

        _inspectorTitle.Text = "⚙️ " + entity.Name;
        if (_posXInput != null) _posXInput.Text = entity.Transform.Position.X.ToString("F2");
        if (_posYInput != null) _posYInput.Text = entity.Transform.Position.Y.ToString("F2");
        if (_posZInput != null) _posZInput.Text = entity.Transform.Position.Z.ToString("F2");
    }

    private AndroidWidgets.Button CreateButton(string text)
    {
        var btn = new AndroidWidgets.Button(this) { Text = text };
        btn.SetTextColor(AndroidGraphics.Color.White);
        btn.SetBackgroundColor(AndroidGraphics.Color.Argb(255, 60, 60, 60));
        return btn;
    }

    private AndroidWidgets.EditText CreateNumberInput(string val)
    {
        var edit = new AndroidWidgets.EditText(this) { Text = val };
        edit.SetTextColor(AndroidGraphics.Color.White);
        edit.SetBackgroundColor(AndroidGraphics.Color.Argb(150, 20, 20, 20));
        edit.InputType = AndroidText.InputTypes.ClassNumber | AndroidText.InputTypes.NumberFlagDecimal | AndroidText.InputTypes.NumberFlagSigned;
        return edit;
    }

    private void ShowCrashDialog(string error)
    {
        new AndroidApp.AlertDialog.Builder(this)
            .SetTitle("Editor Exception")
            .SetMessage(error)
            .SetPositiveButton("OK", (s, e) => { })
            .Show();
    }

    protected override void OnDestroy()
    {
        _editor?.Dispose();
        _editor = null;
        base.OnDestroy();
    }
}                    _editor.OnHierarchyChanged += (entities) =>
                    {
                        RunOnUiThread(() => RefreshHierarchyUI(entities));
                    };

                    _editor.OnEntitySelected += (entity) =>
                    {
                        RunOnUiThread(() => RefreshInspectorUI(entity));
                    };

                    // Mag-inject ng Editor UI Layout sa screen pagkasimula ng window
                    RunOnUiThread(() => BuildEditorOverlay());

                    // Blocking call ng Stride graphics pipeline (sa background thread lamang!)
                    _editor.Run();
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("native window", StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
                {
                    attempt++;
                    Log.Warn("StrideEditor", $"Inihahanda ang Surface... Retry {attempt}/{maxRetries}");
                    _editor?.Dispose();
                    _editor = null;
                }
                catch (Exception ex)
                {
                    Log.Error("StrideEditorCrash", ex.ToString());
                    RunOnUiThread(() => ShowCrashDialog(ex.ToString()));
                    break;
                }
            }
        });
    }

    // ==========================================
    // STRIDE EDITOR INTERFACE (Android UI OVERLAY)
    // ==========================================

    private void BuildEditorOverlay()
    {
        var rootLayout = new RelativeLayout(this)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // 1. TOP TOOLBAR
        var toolbar = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };
        toolbar.SetBackgroundColor(Android.Graphics.Color.Argb(220, 25, 25, 25));
        toolbar.SetPadding(20, 10, 20, 10);

        var btnAddCube = CreateButton("+ Cube");
        btnAddCube.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Cube, "Cube_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        var btnAddSphere = CreateButton("+ Sphere");
        btnAddSphere.Click += (s, e) => _editor?.CreatePrimitive(EditorGame.PrimitiveType.Sphere, "Sphere_" + DateTime.Now.Second, new Vector3(0, 1, 0), Vector3.One);

        toolbar.AddView(btnAddCube);
        toolbar.AddView(btnAddSphere);

        var toolbarParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        toolbarParams.AddRule(LayoutRules.AlignParentTop);
        rootLayout.AddView(toolbar, toolbarParams);

        // 2. LEFT PANEL: SCENE HIERARCHY
        _hierarchyPanel = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        _hierarchyPanel.SetBackgroundColor(Android.Graphics.Color.Argb(200, 35, 35, 35));
        _hierarchyPanel.SetPadding(20, 20, 20, 20);

        var hierarchyTitle = new TextView(this) { Text = "📂 SCENE HIERARCHY", TextSize = 14 };
        hierarchyTitle.SetTextColor(Android.Graphics.Color.Yellow);
        _hierarchyPanel.AddView(hierarchyTitle);

        var hierarchyScroll = new ScrollView(this);
        hierarchyScroll.AddView(_hierarchyPanel);

        var hierarchyParams = new RelativeLayout.LayoutParams(400, ViewGroup.LayoutParams.MatchParent);
        hierarchyParams.AddRule(LayoutRules.AlignParentLeft);
        hierarchyParams.TopMargin = 100;
        rootLayout.AddView(hierarchyScroll, hierarchyParams);

        // 3. RIGHT PANEL: INSPECTOR
        var inspectorLayout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        inspectorLayout.SetBackgroundColor(Android.Graphics.Color.Argb(200, 35, 35, 35));
        inspectorLayout.SetPadding(20, 20, 20, 20);

        _inspectorTitle = new TextView(this) { Text = "⚙️ INSPECTOR: (No Selection)", TextSize = 14 };
        _inspectorTitle.SetTextColor(Android.Graphics.Color.Cyan);
        inspectorLayout.AddView(_inspectorTitle);

        var posLabel = new TextView(this) { Text = "Position (X, Y, Z):" };
        posLabel.SetTextColor(Android.Graphics.Color.White);
        inspectorLayout.AddView(posLabel);

        _posXInput = CreateNumberInput("0");
        _posYInput = CreateNumberInput("0");
        _posZInput = CreateNumberInput("0");

        var applyBtn = CreateButton("Apply Transform");
        applyBtn.Click += (s, e) =>
        {
            if (float.TryParse(_posXInput.Text, out float x) &&
                float.TryParse(_posYInput.Text, out float y) &&
                float.TryParse(_posZInput.Text, out float z))
            {
                _editor?.UpdateEntityPosition(new Vector3(x, y, z));
            }
        };

        inspectorLayout.AddView(_posXInput);
        inspectorLayout.AddView(_posYInput);
        inspectorLayout.AddView(_posZInput);
        inspectorLayout.AddView(applyBtn);

        var inspectorParams = new RelativeLayout.LayoutParams(420, ViewGroup.LayoutParams.MatchParent);
        inspectorParams.AddRule(LayoutRules.AlignParentRight);
        inspectorParams.TopMargin = 100;
        rootLayout.AddView(inspectorLayout, inspectorParams);

        // Idagdag ang Editor UI overlay sa itaas ng Stride Activity
        AddContentView(rootLayout, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
    }

    private void RefreshHierarchyUI(System.Collections.Generic.List<Entity> entities)
    {
        if (_hierarchyPanel == null) return;

        // Iwan ang title
        while (_hierarchyPanel.ChildCount > 1)
        {
            _hierarchyPanel.RemoveViewAt(1);
        }

        foreach (var entity in entities)
        {
            var itemBtn = new Button(this)
            {
                Text = "📦 " + entity.Name,
                TextSize = 12
            };
            itemBtn.SetBackgroundColor(Android.Graphics.Color.Argb(180, 50, 50, 50));
            itemBtn.SetTextColor(Android.Graphics.Color.White);
            itemBtn.Click += (s, e) => _editor?.SelectEntity(entity);
            _hierarchyPanel.AddView(itemBtn);
        }
    }

    private void RefreshInspectorUI(Entity? entity)
    {
        if (entity == null || _inspectorTitle == null) return;

        _inspectorTitle.Text = "⚙️ " + entity.Name;
        _posXInput!.Text = entity.Transform.Position.X.ToString("F2");
        _posYInput!.Text = entity.Transform.Position.Y.ToString("F2");
        _posZInput!.Text = entity.Transform.Position.Z.ToString("F2");
    }

    private Button CreateButton(string text)
    {
        var btn = new Button(this) { Text = text };
        btn.SetTextColor(Android.Graphics.Color.White);
        btn.SetBackgroundColor(Android.Graphics.Color.Argb(255, 60, 60, 60));
        return btn;
    }

    private EditText CreateNumberInput(string val)
    {
        var edit = new EditText(this) { Text = val };
        edit.SetTextColor(Android.Graphics.Color.White);
        edit.SetBackgroundColor(Android.Graphics.Color.Argb(150, 20, 20, 20));
        edit.InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal | Android.Text.InputTypes.NumberFlagSigned;
        return edit;
    }

    private void ShowCrashDialog(string error)
    {
        new AlertDialog.Builder(this)
            .SetTitle("Editor Exception")
            .SetMessage(error)
            .SetPositiveButton("OK", (s, e) => { })
            .Show();
    }

    protected override void OnDestroy()
    {
        _editor?.Dispose();
        _editor = null;
        base.OnDestroy();
    }
}
