using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Panels;
using Button = Stride.UI.Controls.Button;
using EditText = Stride.UI.Controls.EditText;
using TextBlock = Stride.UI.Controls.TextBlock;

namespace StrideStudio.Mobile.UI
{
    public class EditorUIManager
    {
        public UIPage Page { get; private set; }
        public Grid RootGrid { get; private set; }

        // UI Panels
        public ContentDecorator CodeEditorPanel { get; private set; }
        public EditText CodeInputBox { get; private set; }
        public TextBlock StatusText { get; private set; }

        public event Action? OnPlayClicked;
        public event Action? OnStopClicked;
        public event Action<string>? OnCompileCodeClicked;

        public EditorUIManager(SpriteFont font)
        {
            Page = new UIPage();
            RootGrid = new Grid();
            Page.RootElement = RootGrid;

            BuildTopToolbar(font);
            BuildCodeEditorModal(font);
            BuildStatusOverlay(font);
        }

        private void BuildTopToolbar(SpriteFont font)
        {
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 10, 0, 0)
            };

            // Play Button
            var playBtn = CreateButton("▶ Play", Color.DarkGreen, font);
            playBtn.Click += (s, e) => OnPlayClicked?.Invoke();
            toolbar.Children.Add(playBtn);

            // Stop Button
            var stopBtn = CreateButton("⏹ Stop", Color.DarkRed, font);
            stopBtn.Click += (s, e) => OnStopClicked?.Invoke();
            toolbar.Children.Add(stopBtn);

            // Code Editor Toggle
            var codeBtn = CreateButton("</> C# Script", Color.DarkBlue, font);
            codeBtn.Click += (s, e) => CodeEditorPanel.Visibility = 
                CodeEditorPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            toolbar.Children.Add(codeBtn);

            RootGrid.Children.Add(toolbar);
        }

        private void BuildCodeEditorModal(SpriteFont font)
        {
            var modal = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 600,
                Height = 450,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BackgroundColor = new Color(30, 30, 35, 230)
            };

            var title = new TextBlock
            {
                Text = "C# Runtime Script Editor",
                Font = font,
                TextColor = Color.White,
                Margin = new Thickness(10)
            };
            modal.Children.Add(title);

            // Text box para sa code
            CodeInputBox = new EditText
            {
                Text = @"using Stride.Engine;
using Stride.Core.Mathematics;

public class RotatorScript : SyncScript 
{
    public override void Update() 
    {
        Entity.Transform.Rotation *= Quaternion.RotationY(2.0f * (float)Game.UpdateTime.Elapsed.TotalSeconds);
    }
}",
                Font = font,
                TextColor = Color.LightGreen,
                Height = 300,
                Width = 580,
                Margin = new Thickness(10),
                BackgroundColor = new Color(15, 15, 20, 255)
            };
            modal.Children.Add(CodeInputBox);

            var compileBtn = CreateButton("Build & Attach to Entity", Color.OrangeRed, font);
            compileBtn.Click += (s, e) => OnCompileCodeClicked?.Invoke(CodeInputBox.Text);
            modal.Children.Add(compileBtn);

            CodeEditorPanel = new ContentDecorator
            {
                Content = modal,
                Visibility = Visibility.Collapsed
            };
            RootGrid.Children.Add(CodeEditorPanel);
        }

        private void BuildStatusOverlay(SpriteFont font)
        {
            StatusText = new TextBlock
            {
                Text = "Mode: EDITING | Entities: 2",
                Font = font,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 20, 0)
            };
            RootGrid.Children.Add(StatusText);
        }

        private Button CreateButton(string text, Color bg, SpriteFont font)
        {
            var btn = new Button
            {
                BackgroundColor = bg,
                Margin = new Thickness(5),
                Padding = new Thickness(15, 8, 15, 8)
            };
            var label = new TextBlock { Text = text, Font = font, TextColor = Color.White };
            btn.Content = label;
            return btn;
        }
    }
}
