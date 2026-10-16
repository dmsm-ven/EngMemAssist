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

    /// <summary>Тонкие горизонтальные штрихи пролетают справа налево — как «линии скорости» в аниме.</summary>
    private void SpawnSpeedLines(double width, double height)
    {
        var random = new Random();
        var color = TryFindResource("SystemAccentColorPrimary") is Color accent ? accent : Colors.Gray;

        for (var i = 0; i < 18; i++)
        {
            var length = random.Next(140, 420);
            var line = new Rectangle
            {
                Width = length,
                Height = random.NextDouble() * 2 + 1,
                RadiusX = 1,
                RadiusY = 1,
                Opacity = 0,
                Fill = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 0),
                        new GradientStop(Color.FromArgb(200, color.R, color.G, color.B), 1),
                    },
                    new Point(0, 0), new Point(1, 0)),
            };

            Canvas.SetTop(line, random.NextDouble() * height);
            Canvas.SetLeft(line, width);
            SpeedLines.Children.Add(line);

            var delay = TimeSpan.FromMilliseconds(random.Next(0, 140));
            var flight = new Duration(TimeSpan.FromMilliseconds(random.Next(260, 420)));

            var move = new DoubleAnimation(width, -length, flight) { BeginTime = delay };
            move.Completed += (_, _) => SpeedLines.Children.Remove(line);
            line.BeginAnimation(Canvas.LeftProperty, move);

            var fade = new DoubleAnimationUsingKeyFrames { BeginTime = delay, Duration = flight };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, KeyTime.FromPercent(0.25)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            line.BeginAnimation(OpacityProperty, fade);
        }
    }
}
