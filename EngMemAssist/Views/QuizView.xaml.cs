using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EngMemAssist.ViewModels;

namespace EngMemAssist.Views;

public partial class QuizView : UserControl
{
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(380));

    private QuizViewModel? _subscribed;
    private Window? _window;

    public QuizView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        Loaded += (_, _) => AttachKeyboard();
        Unloaded += (_, _) =>
        {
            Unsubscribe();
            DetachKeyboard();
        };
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (DataContext is QuizViewModel vm)
        {
            _subscribed = vm;
            vm.ShakeRequested += OnShakeRequested;
            vm.QuestionChanging += OnQuestionChanging;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.ShakeRequested -= OnShakeRequested;
            _subscribed.QuestionChanging -= OnQuestionChanging;
            _subscribed = null;
        }
    }

    // ---------- Клавиши A, B, C… выбирают соответствующий вариант ----------

    private void AttachKeyboard()
    {
        DetachKeyboard();
        _window = Window.GetWindow(this);
        if (_window is not null)
        {
            _window.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private void DetachKeyboard()
    {
        if (_window is not null)
        {
            _window.PreviewKeyDown -= OnPreviewKeyDown;
            _window = null;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is < Key.A or > Key.Z || Keyboard.Modifiers != ModifierKeys.None || DataContext is not QuizViewModel vm)
        {
            return;
        }

        var index = e.Key - Key.A;
        if (index < vm.VisibleOptions.Count)
        {
            var option = vm.VisibleOptions[index];
            if (vm.SelectOptionCommand.CanExecute(option))
            {
                vm.SelectOptionCommand.Execute(option);
                e.Handled = true;
            }
        }
    }

    // ---------- Встряска при ошибке ----------

    private void OnShakeRequested(object? sender, EventArgs e)
    {
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(420) };
        double[] offsets = [0, -12, 11, -9, 7, -4, 2, 0];
        for (var i = 0; i < offsets.Length; i++)
        {
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(
                offsets[i], KeyTime.FromPercent((double)i / (offsets.Length - 1))));
        }

        ShakeTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    // ---------- Смена вопроса: старый уезжает влево, новый въезжает справа, линии скорости ----------

    /// <summary>Вызывается до смены данных: снимаем картинку текущего вопроса и запускаем анимации.</summary>
    private void OnQuestionChanging(object? sender, EventArgs e)
    {
        var width = ContentLayer.ActualWidth;
        var height = ContentLayer.ActualHeight;
        if (width < 1 || height < 1)
        {
            return;
        }

        try
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(width * dpi.DpiScaleX), (int)Math.Ceiling(height * dpi.DpiScaleY),
                96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
            bitmap.Render(ContentLayer);

            OutgoingImage.Source = bitmap;
            OutgoingImage.Width = width;
            OutgoingImage.Height = height;
            OutgoingImage.Visibility = Visibility.Visible;
        }
        catch (Exception)
        {
            return; // анимация — украшение, при сбое просто меняем вопрос без неё
        }

        // Новое содержимое сразу уводим за правый край; дальше изменятся данные, и оно выедет.
        ContentShift.X = width;

        var leave = new DoubleAnimation(0, -width, SlideDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        leave.Completed += (_, _) =>
        {
            OutgoingImage.Visibility = Visibility.Collapsed;
            OutgoingImage.Source = null;
        };
        OutgoingShift.BeginAnimation(TranslateTransform.XProperty, leave);

        var enter = new DoubleAnimation(width, 0, SlideDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        enter.Completed += (_, _) =>
        {
            ContentShift.BeginAnimation(TranslateTransform.XProperty, null);
            ContentShift.X = 0;
        };
        ContentShift.BeginAnimation(TranslateTransform.XProperty, enter);

        SpawnSpeedLines(width, height);
    }

    /// <summary>Спрайты ветра (прозрачные PNG с изогнутыми штрихами и завитками), загружаются один раз.</summary>
    private static readonly Lazy<BitmapImage[]> WindSprites = new(() =>
        Enumerable.Range(1, 8).Select(i =>
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"pack://application:,,,/Assets/Wind/wind{i}.png", UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }).ToArray());

    /// <summary>
    /// Воздушные потоки: изогнутые штрихи разного размера и наклона пролетают в основном справа налево,
    /// часть из них отражена и летит под другим углом или навстречу. Цвет берётся из акцента темы.
    /// </summary>
    private void SpawnSpeedLines(double width, double height)
    {
        var random = new Random();
        var sprites = WindSprites.Value;
        var color = TryFindResource("SystemAccentColorPrimary") is Color accent ? accent : Colors.Gray;
        var tint = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        tint.Freeze();

        for (var i = 0; i < 16; i++)
        {
            var sprite = sprites[random.Next(sprites.Length)];
            var w = random.Next(280, 720);
            var h = w / 4.0;
            var reverse = random.NextDouble() < 0.2;
            var angle = random.NextDouble() * 44 - 22;           // наклон потока, градусы
            var flipY = random.Next(2) == 0 ? -1 : 1;             // зеркальное отражение — другой изгиб
            var flipX = reverse ? -1 : (random.Next(4) == 0 ? -1 : 1);

            var scale = new ScaleTransform(flipX, flipY);
            var rotate = new RotateTransform(reverse ? -angle : angle);
            var shift = new TranslateTransform();

            var piece = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = tint,
                OpacityMask = new ImageBrush(sprite) { Stretch = Stretch.Fill },
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new TransformGroup { Children = { scale, rotate, shift } },
            };

            Canvas.SetLeft(piece, 0);
            Canvas.SetTop(piece, random.NextDouble() * height - h / 2);
            SpeedLines.Children.Add(piece);

            // Движение вдоль оси спрайта: основной поток — справа налево, «встречные» — медленнее и бледнее.
            var distance = width + w;
            var dx = reverse ? distance : -distance;
            var startX = reverse ? -w : width;
            var dy = dx * Math.Tan((reverse ? -angle : angle) * Math.PI / 180) * (reverse ? -1 : 1);

            var delay = TimeSpan.FromMilliseconds(random.Next(0, 180));
            var flight = new Duration(TimeSpan.FromMilliseconds(reverse ? random.Next(550, 800) : random.Next(380, 620)));

            var move = new DoubleAnimation(startX, startX + dx, flight) { BeginTime = delay };
            move.Completed += (_, _) => SpeedLines.Children.Remove(piece);
            shift.BeginAnimation(TranslateTransform.XProperty, move);
            shift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, dy, flight) { BeginTime = delay });

            var peak = reverse ? 0.35 : 0.8;
            var fade = new DoubleAnimationUsingKeyFrames { BeginTime = delay, Duration = flight };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(peak, KeyTime.FromPercent(0.3)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(peak * 0.7, KeyTime.FromPercent(0.7)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            piece.BeginAnimation(OpacityProperty, fade);
        }
    }
}
