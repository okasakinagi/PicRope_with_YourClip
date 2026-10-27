using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenLine
{
    internal sealed class ClothesClip : Canvas
    {
        // 角色图高度与底边（卡片坐标）。底边压在照片上沿下方 8px，向上溢出画布，
        // 因此不必改动 ShelfWindow 里的挂点与点击热区。
        const double PortraitHeight = 44, PortraitBottom = 35;

        readonly RotateTransform front = new RotateTransform(0, 10, 14), back = new RotateTransform(0, 10, 14);
        readonly ScaleTransform portraitScale = new ScaleTransform(1, 1);
        bool opened, portrait;
        public bool UsePortrait { get; set; }
        public LineTheme Appearance { get; private set; }

        public ClothesClip() : this(LineTheme.All[0], 0) { }

        public ClothesClip(LineTheme theme, int colorIndex)
        {
            Width = 16; Height = 30; IsHitTestVisible = false;
            UsePortrait = true;
            Apply(theme, colorIndex);
        }

        public void Apply(LineTheme theme, int colorIndex)
        {
            Appearance = theme;
            Children.Clear();
            portraitScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            portraitScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            portrait = UsePortrait && ClipArt.Count > 0;
            if (portrait) { ApplyPortrait(ClipArt.For(colorIndex)); return; }
            string color = theme.ClipColors[colorIndex % theme.ClipColors.Length];
            bool wood = theme.Id == "linen", metal = theme.Id == "classic" || theme.Id == "graphite";
            double x = wood ? 1.5 : (theme.Id == "color" ? 1.5 : 3), width = wood ? 10 : (theme.Id == "color" ? 11.5 : 9.5);
            double height = theme.Id == "classic" || theme.Id == "graphite" ? 27 : 30;
            string edge = wood ? "#AD8E69" : (theme.Id == "graphite" ? "#424950" : (theme.Id == "classic" ? "#8A98AA" : (theme.Id == "porcelain" ? "#C2BFB5" : "#B4B3A9")));
            var rear = new Canvas { Width = 16, Height = 30, RenderTransform = back };
            rear.Children.Add(new Rectangle {
                Width = width - 1, Height = height - 0.5, RadiusX = wood ? 1.8 : 3, RadiusY = wood ? 1.8 : 3,
                Fill = Shade(color, -0.13), Stroke = LightTheme.Brush(edge), StrokeThickness = 0.5
            });
            SetLeft(rear.Children[0], x + 2.7); SetTop(rear.Children[0], 0.5);
            Children.Add(rear);
            var face = new Canvas { Width = 16, Height = 30, RenderTransform = front };
            var finish = new LinearGradientBrush();
            finish.StartPoint = new Point(0, 0); finish.EndPoint = new Point(1, 0.3);
            finish.GradientStops.Add(new GradientStop(((SolidColorBrush)Shade(color, metal ? 0.36 : 0.18)).Color, 0));
            finish.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(color), 0.55));
            finish.GradientStops.Add(new GradientStop(((SolidColorBrush)Shade(color, theme.Id == "porcelain" ? -0.04 : -0.08)).Color, 1));
            finish.Freeze();
            face.Children.Add(new Rectangle {
                Width = width, Height = height, RadiusX = wood ? 1.8 : 3, RadiusY = wood ? 1.8 : 3,
                Fill = finish, Stroke = LightTheme.Brush(edge), StrokeThickness = wood ? 0.6 : 0.55
            });
            SetLeft(face.Children[0], x);
            if (wood)
            {
                for (int i = 0; i < 3; i++)
                    face.Children.Add(new Path {
                        Data = Geometry.Parse("M" + (x + 2 + i * 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",3 q-0.6,4 0.1,8 m0,6 q0.6,4 -0.2,10"),
                        Stroke = LightTheme.Brush(i == 1 ? "#C5A87F" : "#F3E2C8"), StrokeThickness = 0.35, Opacity = 0.65
                    });
                face.Children.Add(new Line { X1 = x + 0.8, X2 = x + width - 0.8, Y1 = 16.2, Y2 = 16.2,
                    Stroke = LightTheme.Brush("#AB8E6D"), StrokeThickness = 0.65 });
            }
            else
                face.Children.Add(new Line { X1 = x + 1.2, X2 = x + 1.2, Y1 = 3.5, Y2 = height - 3.5,
                    Stroke = Shade(color, 0.48), StrokeThickness = 0.55, Opacity = metal ? 0.6 : 0.3 });
            if (theme.Id == "color")
                face.Children.Add(new Line { X1 = x + width * 0.55, X2 = x + width * 0.55, Y1 = 2.5, Y2 = height - 3,
                    Stroke = Shade(color, -0.25), StrokeThickness = 0.55, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            Children.Add(face);
            var hinge = new Ellipse { Width = wood ? 3.2 : 2.2, Height = wood ? 3.8 : 2.6,
                Fill = LightTheme.Brush(metal ? "#ADB5BF" : "#E0DFD7"), Stroke = LightTheme.Brush(wood ? "#7D807B" : edge), StrokeThickness = 0.55 };
            Children.Add(hinge); SetLeft(hinge, x + width - 0.4); SetTop(hinge, 12.5);
            if (wood)
            {
                Children.Add(new Path { Data = Geometry.Parse("M12.3,14.5 Q15.3,12.8 14.2,16 L12,19 H3"),
                    Stroke = LightTheme.Brush("#8A8C85"), StrokeThickness = 0.8, StrokeLineJoin = PenLineJoin.Round });
            }
            front.BeginAnimation(RotateTransform.AngleProperty, null); back.BeginAnimation(RotateTransform.AngleProperty, null);
            front.Angle = opened ? 7 : 0; back.Angle = opened ? -5 : 0;
        }

        void ApplyPortrait(BitmapSource source)
        {
            double box = ClipArt.BoxFor(PortraitHeight);
            var image = new Image {
                Source = source, Width = box, Height = box, Stretch = Stretch.Fill, IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, ClipArt.BottomFraction), RenderTransform = portraitScale
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            SetLeft(image, (Width - box) / 2); SetTop(image, PortraitBottom - box * ClipArt.BottomFraction);
            Children.Add(image);
            portraitScale.ScaleX = portraitScale.ScaleY = opened ? 1.08 : 1;
        }

        static Brush Shade(string value, double amount)
        {
            Color color = (Color)ColorConverter.ConvertFromString(value);
            Func<byte, byte> shift = channel => (byte)Math.Round(amount >= 0 ? channel + (255 - channel) * amount : channel * (1 + amount));
            var brush = new SolidColorBrush(Color.FromRgb(shift(color.R), shift(color.G), shift(color.B)));
            brush.Freeze(); return brush;
        }

        public void Open()
        {
            opened = true;
            if (portrait)
            {
                var lift = new DoubleAnimation(1.1, TimeSpan.FromSeconds(0.10)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                portraitScale.BeginAnimation(ScaleTransform.ScaleXProperty, lift);
                portraitScale.BeginAnimation(ScaleTransform.ScaleYProperty, lift);
                return;
            }
            front.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(7, TimeSpan.FromSeconds(0.10)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            back.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-5, TimeSpan.FromSeconds(0.10)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        public void Reset()
        {
            opened = false;
            if (portrait)
            {
                portraitScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                portraitScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                portraitScale.ScaleX = portraitScale.ScaleY = 1;
                return;
            }
            front.BeginAnimation(RotateTransform.AngleProperty, null); back.BeginAnimation(RotateTransform.AngleProperty, null);
            front.Angle = back.Angle = 0;
        }
    }
}
