using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace StrideStudio.Mobile
{
    public class ScriptEditorUI
    {
        private readonly EditorGame _game;
        public UIComponent UIComponent { get; private set; }
        private ModalPanel? _scriptAddModal;
        private TextBlock? _statusText;

        public ScriptEditorUI(EditorGame game)
        {
            _game = game;
            UIComponent = new UIComponent();
            BuildEditorOverlay();
        }

        private void BuildEditorOverlay()
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { SizeValue = 1, SizeType = StripType.Star });

            // Top Toolbar Panel
            var topBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(20, 20, 0, 0)
            };

            // 1. Play / Edit Mode Toggle Button
            var playBtn = CreateStyledButton("▶ Play / Edit", new Color(40, 160, 60));
            playBtn.Click += (s, e) =>
            {
                _game.IsPlaying = !_game.IsPlaying;
                UpdateStatus(_game.IsPlaying ? "STATUS: PLAYING (Physics Active)" : "STATUS: EDIT MODE");
            };
            topBar.Children.Add(playBtn);

            // 2. [+ Add Script] Button
            var addScriptBtn = CreateStyledButton("+ Add Script", new Color(30, 120, 220));
            addScriptBtn.Click += (s, e) => OpenAddScriptDialog();
            topBar.Children.Add(addScriptBtn);

            // 3. [+ Add Visual Node] Button
            var addNodeBtn = CreateStyledButton("+ Add Node", new Color(180, 100, 30));
            addNodeBtn.Click += (s, e) =>
            {
                // Dynamic Node Addition sa runtime graph
                _game.AddDynamicNode(new ApplyPhysicsImpulseNode(new Vector3(0, 7f, 0)));
                UpdateStatus("Node Added: Apply Physics Jump Impulse!");
            };
            topBar.Children.Add(addNodeBtn);

            // Status Bar Indicator
            _statusText = new TextBlock
            {
                Text = "STATUS: EDIT MODE (Tap '+ Add Script' to attach)",
                TextColor = Color.White,
                TextSize = 20,
                Margin = new Thickness(20, 10, 0, 0)
            };
            topBar.Children.Add(_statusText);

            rootGrid.Children.Add(topBar);

            var page = new UIPage { RootElement = rootGrid };
            UIComponent.Page = page;
        }

        private void OpenAddScriptDialog()
        {
            if (_game.SelectedEntity == null)
            {
                UpdateStatus("Pumili muna ng Entity bago mag-add ng script!");
                return;
            }

            var rootGrid = (Grid)UIComponent.Page.RootElement;

            // Modal Background
            var modalBox = new Border
            {
                BackgroundColor = new Color(20, 20, 25, 235),
                Width = 500,
                Height = 400,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var modalStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(15)
            };

            var title = new TextBlock
            {
                Text = "SCRIPT BROWSER & ATTACH",
                TextSize = 24,
                TextColor = Color.Yellow,
                Margin = new Thickness(0, 0, 0, 15)
            };
            modalStack.Children.Add(title);

            // Ilista ang bawat available script mula sa registry
            foreach (var scriptMeta in ScriptRegistry.AvailableScripts)
            {
                var itemBtn = CreateStyledButton($"{scriptMeta.Name} - {scriptMeta.Description}", new Color(50, 50, 65));
                itemBtn.Width = 460;
                itemBtn.Margin = new Thickness(0, 5, 0, 5);

                itemBtn.Click += (s, e) =>
                {
                    ScriptRegistry.AttachScript(_game.SelectedEntity, scriptMeta);
                    UpdateStatus($"Attached: {scriptMeta.Name} to {_game.SelectedEntity.Name}");
                    rootGrid.Children.Remove(modalBox);
                };

                modalStack.Children.Add(itemBtn);
            }

            // Close button
            var closeBtn = CreateStyledButton("Cancel", new Color(180, 40, 40));
            closeBtn.Click += (s, e) => rootGrid.Children.Remove(modalBox);
            modalStack.Children.Add(closeBtn);

            modalBox.Content = modalStack;
            rootGrid.Children.Add(modalBox);
        }

        public void UpdateStatus(string message)
        {
            if (_statusText != null) _statusText.Text = message;
        }

        private Button CreateStyledButton(string text, Color bgColor)
        {
            var btn = new Button
            {
                BackgroundColor = bgColor,
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(5, 0, 5, 0)
            };

            var tb = new TextBlock
            {
                Text = text,
                TextSize = 18,
                TextColor = Color.White
            };
            btn.Content = tb;
            return btn;
        }
    }
}
