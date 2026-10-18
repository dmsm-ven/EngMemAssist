using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace EngMemAssist.Views;

public enum ToastResult
{
    /// <summary>Нажали «Пройти».</summary>
    Accepted,

    /// <summary>Нажали «Позже».</summary>
    Snoozed,

    /// <summary>Закрыли крестиком или окно исчезло по таймеру — ответа не было.</summary>
    Ignored,
}

/// <summary>Всплывающее окно у трея. Не забирает фокус у текущего приложения и сам исчезает через ~25 секунд.</summary>
public partial class ReminderToastWindow : Window
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(25);

    private readonly TaskCompletionSource<ToastResult> _completion = new();
    private readonly DispatcherTimer _timer = new() { Interval = Lifetime };
    private ToastResult _result = ToastResult.Ignored;

    public ReminderToastWindow()
    {
        InitializeComponent();
        Opacity = 0;
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _timer.Stop();
            _completion.TrySetResult(_result);
        };
        _timer.Tick += (_, _) => Close();
    }

    /// <summary>Показывает окно и ждёт, пока оно закроется.</summary>
    public Task<ToastResult> ShowAsync()
    {
        Show();
        return _completion.Task;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth;
        Top = area.Bottom - ActualHeight;

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
        _timer.Start();
    }

    private void OnAccept(object sender, RoutedEventArgs e) => Finish(ToastResult.Accepted);

    private void OnSnooze(object sender, RoutedEventArgs e) => Finish(ToastResult.Snoozed);

    private void OnDismiss(object sender, RoutedEventArgs e) => Finish(ToastResult.Ignored);

    private void Finish(ToastResult result)
    {
        _result = result;
        Close();
    }
}
