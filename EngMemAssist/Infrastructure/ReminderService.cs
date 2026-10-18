using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using EngMemAssist.Navigation;
using EngMemAssist.ViewModels;
using EngMemAssist.Views;
using System.Windows;
using System.Windows.Threading;

namespace EngMemAssist.Infrastructure;

/// <summary>
/// Периодически проверяет, можно ли предложить мини-тест, и показывает окно-уведомление.
/// Решение принимает <see cref="ReminderPolicy"/>; здесь собирается обстановка (полноэкранные приложения, простой, окно приложения).
/// </summary>
public sealed class ReminderService : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SnoozeDelay = TimeSpan.FromMinutes(15);

    private readonly ISettingsService _settings;
    private readonly IWordRepository _words;
    private readonly INavigationService _navigation;
    private readonly SyncCoordinator _sync;
    private readonly AppShell _shell;
    private readonly DispatcherTimer _timer;
    private readonly DateTime _startedUtc = DateTime.UtcNow;

    private int _ignored;
    private bool _busy;

    public ReminderService(
        ISettingsService settings, IWordRepository words, INavigationService navigation, SyncCoordinator sync, AppShell shell)
    {
        _settings = settings;
        _words = words;
        _navigation = navigation;
        _sync = sync;
        _shell = shell;
        _timer = new DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();

    /// <summary>Показать окно сразу, мимо всех проверок и не считая в суточный лимит (кнопка проверки в настройках).</summary>
    public async Task ShowTestAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            var result = await new ReminderToastWindow().ShowAsync();
            if (result == ToastResult.Accepted)
            {
                _shell.StartQuickQuiz();
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        try
        {
            await CheckAsync();
        }
        catch (Exception)
        {
            // Напоминание не должно ронять приложение; следующая проверка через 30 секунд.
        }
    }

    private async Task CheckAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            var settings = _settings.Current;
            if (ReminderPolicy.Evaluate(settings, BuildContext(settings)) != ReminderVerdict.Show)
            {
                return;
            }

            if (_sync.IsBusy || await _words.CountActiveAsync() == 0)
            {
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Now);
            ReminderPolicy.RegisterShown(settings, DateTime.UtcNow, today);
            _settings.Save();

            var result = await new ReminderToastWindow().ShowAsync();
            switch (result)
            {
                case ToastResult.Accepted:
                    _ignored = 0;
                    _shell.StartQuickQuiz();
                    break;

                case ToastResult.Snoozed:
                    ReminderPolicy.Snooze(settings, DateTime.UtcNow, today, SnoozeDelay);
                    _settings.Save();
                    break;

                default:
                    _ignored++;
                    break;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private ReminderContext BuildContext(AppSettings settings)
    {
        var baseline = settings.LastReminderAt is { } last && last > _startedUtc ? last : _startedUtc;

        var window = Application.Current.MainWindow;
        var appBusy = _navigation.Current is QuizViewModel || window is { IsVisible: true, IsActive: true };

        return new ReminderContext(
            DateTime.UtcNow,
            DateTime.Now,
            baseline,
            appBusy,
            UserActivityMonitor.IsNotificationBlocked(),
            UserActivityMonitor.GetIdleTime(),
            _ignored);
    }

    public void Dispose() => _timer.Stop();
}
