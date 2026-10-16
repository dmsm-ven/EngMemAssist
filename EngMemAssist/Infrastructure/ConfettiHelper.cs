using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EngMemAssist.Infrastructure;

/// <summary>Конфетти: цветные кусочки падают сверху вниз по Canvas и сами исчезают.</summary>
public static class ConfettiHelper
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(0xFF, 0x6B, 0x6B), Color.FromRgb(0xFF, 0xD9, 0x3D), Color.FromRgb(0x6B, 0xCB, 0x77),
        Color.FromRgb(0x4D, 0x96, 0xFF), Color.FromRgb(0xC7, 0x7D, 0xFF), Color.FromRgb(0xFF, 0x9F, 0x45),
    ];

    public static void Launch(Canvas canvas, int count = 110)
    {
        // Размер Canvas становится известен только после разметки.
        canvas.Dispatcher.BeginInvoke(() => Spawn(canvas, count), DispatcherPriority.Loaded);
    }

    private static void Spawn(Canvas canvas, int count)
    {
        var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800;
        var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 500;
        var random = new Random();

        for (var i = 0; i < count; i++)
        {
            var piece = new Rectangle
            {
                Width = random.Next(6, 11),
                Height = random.Next(10, 17),
                Fill = new SolidColorBrush(Palette[random.Next(Palette.Length)]),
                RadiusX = 1,
                RadiusY = 1,
                RenderTransformOrigin = new Point(0.5, 0.5),
            };

            var rotate = new RotateTransform(random.Next(0, 360));
            var sway = new TranslateTransform();
            piece.RenderTransform = new TransformGroup { Children = { rotate, sway } };

            Canvas.SetLeft(piece, random.NextDouble() * width);
            Canvas.SetTop(piece, -30);
            canvas.Children.Add(piece);

            var delay = TimeSpan.FromMilliseconds(random.Next(0, 900));
            var fall = TimeSpan.FromMilliseconds(random.Next(2200, 4200));

            piece.BeginAnimation(Canvas.TopProperty,
                new DoubleAnimation(-30, height + 30, fall) { BeginTime = delay });
            rotate.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(rotate.Angle, rotate.Angle + random.Next(240, 720), fall) { BeginTime = delay });
            sway.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(-20, 20, TimeSpan.FromMilliseconds(random.Next(500, 900)))
                {
                    BeginTime = delay,
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
        }

        // Через несколько секунд убираем всё, что упало.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            canvas.Children.Clear();
        };
        timer.Start();
    }
}
