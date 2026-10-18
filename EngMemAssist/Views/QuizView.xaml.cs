using EngMemAssist.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace EngMemAssist.Views;

public partial class QuizView : UserControl
{
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(600));
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

    // ---------- Клавиши A, B, C… ставят фокус на вариант, повтор — выбирают ----------

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

    /// <summary>
    /// Первое нажатие буквы только ставит фокус на вариант, повторное нажатие той же буквы выбирает его
    /// (Enter и пробел на кнопке с фокусом работают штатно). Стрелки и Tab тоже перемещают фокус между вариантами.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is < Key.A or > Key.Z || Keyboard.Modifiers != ModifierKeys.None || DataContext is not QuizViewModel vm)
        {
            return;
        }

        var index = e.Key - Key.A;
        if (index >= vm.VisibleOptions.Count ||
            OptionsList.ItemContainerGenerator.ContainerFromIndex(index) is not DependencyObject container ||
            FindButton(container) is not { } button)
        {
            return;
        }

        e.Handled = true;
        if (e.IsRepeat)
        {
            return; // удержание клавиши не должно выбирать вариант
        }

        if (button.IsKeyboardFocused)
        {
            var option = vm.VisibleOptions[index];
            if (vm.SelectOptionCommand.CanExecute(option))
            {
                vm.SelectOptionCommand.Execute(option);
            }
        }
        else
        {
            button.Focus();
        }
    }

    private static Button? FindButton(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button button)
            {
                return button;
            }

            if (FindButton(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
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

        var leave = new DoubleAnimation(0, -width, SlideDuration)
        {
            EasingFunction = new CubicEase
            {
                EasingMode = EasingMode.EaseIn
            }
        };
        leave.Completed += (_, _) =>
        {
            OutgoingImage.Visibility = Visibility.Collapsed;
            OutgoingImage.Source = null;
        };
        OutgoingShift.BeginAnimation(TranslateTransform.XProperty, leave);

        var enter = new DoubleAnimation(width, 0, SlideDuration)
        {
            EasingFunction = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            }
        };
        enter.Completed += (_, _) =>
        {
            ContentShift.BeginAnimation(TranslateTransform.XProperty, null);
            ContentShift.X = 0;
        };
        ContentShift.BeginAnimation(TranslateTransform.XProperty, enter);
    }
}
