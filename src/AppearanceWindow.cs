using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenLine
{
    internal sealed class AppearanceWindow : Window
    {
        readonly Action<LineTheme> apply;
        readonly Gallery gallery;
        readonly List<TextBlock> selectionMarks = new List<TextBlock>();
        readonly Button portraitButton, vectorButton;
        internal readonly List<Button> Options = new List<Button>();
        LineTheme current;

        public AppearanceWindow(LineTheme selected, Gallery gallery, Action<LineTheme> apply)
        {
            this.apply = apply;
            this.gallery = gallery;
            current = selected;
            Title = "外观";
            Width = 654; Height = 516;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; Topmost = true;
            FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var body = new StackPanel();
            var heading = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            heading.Children.Add(new TextBlock { Text = "外观", FontSize = 21, FontWeight = FontWeights.SemiBold, Foreground = LightTheme.Brush("#283343") });
            var close = LightTheme.Button("", Close);
            close.Content = LightTheme.CloseIcon(); close.Width = close.Height = 28; close.MinWidth = 0;
            close.Padding = new Thickness(0); close.Tag = "quiet"; close.HorizontalAlignment = HorizontalAlignment.Right;
            System.Windows.Automation.AutomationProperties.SetName(close, "关闭外观");
            heading.Children.Add(close);
            heading.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs args) { if (args.OriginalSource is TextBlock) DragMove(); };
            body.Children.Add(heading);
            body.Children.Add(new TextBlock { Text = "绳子与夹子整套切换", FontSize = 12, Foreground = LightTheme.Brush("#65717F"), Margin = new Thickness(0, 0, 0, 18) });
            var styleRow = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            styleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            styleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            styleRow.Children.Add(new TextBlock { Text = "夹子样式", FontSize = 12, Foreground = LightTheme.Brush("#65717F"), VerticalAlignment = VerticalAlignment.Center });
            var styleButtons = new StackPanel { Orientation = Orientation.Horizontal };
            portraitButton = LightTheme.Button("角色头像", delegate { ChooseStyle(false); });
            vectorButton = LightTheme.Button("原版夹子", delegate { ChooseStyle(true); });
            vectorButton.Margin = new Thickness(8, 0, 0, 0);
            System.Windows.Automation.AutomationProperties.SetAutomationId(portraitButton, "clip-style-portrait");
            System.Windows.Automation.AutomationProperties.SetAutomationId(vectorButton, "clip-style-vector");
            if (ClipArt.Available == 0)
            {
                portraitButton.IsEnabled = false;
                portraitButton.ToolTip = new ToolTip { Content = "当前程序没有内置角色图，把图片放进 yourclip\\images 后重新生成素材", Style = LightTheme.HintStyle };
            }
            styleButtons.Children.Add(portraitButton); styleButtons.Children.Add(vectorButton);
            Grid.SetColumn(styleButtons, 1); styleRow.Children.Add(styleButtons);
            body.Children.Add(styleRow);
            UpdateStyleButtons();
            var choices = new WrapPanel();
            foreach (var theme in LineTheme.All)
            {
                var content = new StackPanel();
                content.Children.Add(Preview(theme, gallery));
                var label = new Grid { Margin = new Thickness(11, 0, 11, 5) };
                label.Children.Add(new TextBlock { Text = theme.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = LightTheme.Brush("#283343") });
                var mark = new TextBlock { Text = "已选", FontSize = 10, Foreground = LightTheme.Brush("#2463AE"), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                selectionMarks.Add(mark); label.Children.Add(mark); content.Children.Add(label);
                content.Children.Add(new TextBlock { Text = theme.Description, FontSize = 10,
                    Foreground = LightTheme.Brush("#65717F"), Margin = new Thickness(11, 0, 0, 10) });
                var button = new Button { Content = content, Style = LightTheme.ButtonStyle,
                    Width = 180, Height = 139, Padding = new Thickness(0), Margin = new Thickness(0, 0, 10, 12), Tag = theme };
                System.Windows.Automation.AutomationProperties.SetName(button, theme.Name);
                System.Windows.Automation.AutomationProperties.SetAutomationId(button, "theme-" + theme.Id);
                button.Click += delegate { Select(theme); };
                Options.Add(button); choices.Children.Add(button);
            }
            body.Children.Add(choices);
            var card = LightTheme.Card(body);
            card.Padding = new Thickness(24); Content = card;
            Selected(selected);
            PreviewKeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Escape) Close(); };
        }

        void Select(LineTheme theme)
        {
            current = theme;
            apply(theme);
            Selected(theme);
        }

        void ChooseStyle(bool vector)
        {
            if (gallery.Store.State.VectorClips != vector)
            {
                gallery.Store.State.VectorClips = vector;
                gallery.Store.Save();
                ClipArt.Enabled = !vector;
                UpdateStyleButtons();
            }
            apply(current);
        }

        void UpdateStyleButtons()
        {
            bool vector = gallery.Store.State.VectorClips;
            portraitButton.Tag = vector ? null : "primary";
            vectorButton.Tag = vector ? "primary" : null;
            System.Windows.Automation.AutomationProperties.SetItemStatus(portraitButton, vector ? "" : "已选择");
            System.Windows.Automation.AutomationProperties.SetItemStatus(vectorButton, vector ? "已选择" : "");
        }

        void Selected(LineTheme theme)
        {
            for (int i = 0; i < Options.Count; i++)
            {
                var button = Options[i];
                bool current = button.Tag == theme;
                selectionMarks[i].Visibility = current ? Visibility.Visible : Visibility.Collapsed;
                button.BorderBrush = LightTheme.Brush(current ? "#6E97CB" : "#DCE2EA");
                button.BorderThickness = new Thickness(current ? 2 : 1);
                System.Windows.Automation.AutomationProperties.SetItemStatus(button, current ? "已选择" : "");
            }
        }

        static Canvas Preview(LineTheme theme, Gallery gallery)
        {
            var canvas = new Canvas { Width = 174, Height = 81, IsHitTestVisible = false };
            var rope = new System.Windows.Shapes.Path { Data = Geometry.Parse("M0,20 Q87,30 174,20") };
            RopeLine.Apply(rope, theme); canvas.Children.Add(rope);
            for (int i = 0; i < 3; i++)
            {
                var frame = new Border { Width = 44, Height = 32, CornerRadius = new CornerRadius(4),
                    Background = Brushes.White, BorderBrush = LightTheme.Brush("#DCE2EA"), BorderThickness = new Thickness(0.7), Padding = new Thickness(2) };
                var image = gallery.Items.Skip(i).FirstOrDefault() ?? gallery.Items.FirstOrDefault();
                frame.Child = image == null ? (UIElement)new Border { Background = LightTheme.Brush("#DCE5F1"), CornerRadius = new CornerRadius(2) }
                    : new Image { Source = image.Image.Thumbnail, Stretch = Stretch.Uniform, ClipToBounds = true };
                double x = 6 + i * 59, y = 27 + (i == 1 ? 2 : 0);
                canvas.Children.Add(frame); Canvas.SetLeft(frame, x); Canvas.SetTop(frame, y);
                var clip = new ClothesClip(theme, i) { RenderTransform = new ScaleTransform(0.42, 0.42) };
                canvas.Children.Add(clip); Canvas.SetLeft(clip, x + 22 - 8 * 0.42); Canvas.SetTop(clip, y - 8.4);
            }
            return canvas;
        }
    }
}
