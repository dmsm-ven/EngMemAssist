using EngMemAssist.Core.Settings;

namespace EngMemAssist.Core.Services;

/// <summary>Почему напоминание показано или нет.</summary>
public enum ReminderVerdict
{
    Show,
    Disabled,
    QuietHours,
    LimitReached,
    AppBusy,
    FullscreenApp,
    UserAway,
    TooSoon,
}

/// <summary>Снимок обстановки на момент проверки (собирается слоем UI).</summary>
/// <param name="BaselineUtc">От какого момента отсчитывается интервал: последнее напоминание или запуск приложения.</param>
/// <param name="AppBusy">Квиз уже идёт или окно приложения сейчас на переднем плане.</param>
/// <param name="NotificationsBlocked">Полноэкранное приложение, презентация, «Не беспокоить» Windows, заставка.</param>
/// <param name="ConsecutiveIgnored">Сколько напоминаний подряд закрыто без ответа.</param>
public sealed record ReminderContext(
    DateTime NowUtc,
    DateTime NowLocal,
    DateTime BaselineUtc,
    bool AppBusy,
    bool NotificationsBlocked,
    TimeSpan IdleTime,
    int ConsecutiveIgnored);

/// <summary>Чистая логика «можно ли сейчас побеспокоить пользователя» — без обращений к системе.</summary>
public static class ReminderPolicy
{
    public static readonly TimeSpan AwayThreshold = TimeSpan.FromMinutes(5);

    public static ReminderVerdict Evaluate(AppSettings settings, ReminderContext context)
    {
        if (!settings.RemindersEnabled)
        {
            return ReminderVerdict.Disabled;
        }

        if (IsQuietHour(context.NowLocal.Hour, settings.ReminderQuietFromHour, settings.ReminderQuietToHour))
        {
            return ReminderVerdict.QuietHours;
        }

        if (CountToday(settings, DateOnly.FromDateTime(context.NowLocal)) >= Math.Max(1, settings.ReminderMaxPerDay))
        {
            return ReminderVerdict.LimitReached;
        }

        if (context.NowUtc - context.BaselineUtc < EffectiveInterval(settings, context.ConsecutiveIgnored))
        {
            return ReminderVerdict.TooSoon;
        }

        if (context.AppBusy)
        {
            return ReminderVerdict.AppBusy;
        }

        if (context.NotificationsBlocked)
        {
            return ReminderVerdict.FullscreenApp;
        }

        if (context.IdleTime > AwayThreshold)
        {
            return ReminderVerdict.UserAway;
        }

        return ReminderVerdict.Show;
    }

    /// <summary>Интервал растёт, если пользователь раз за разом игнорирует напоминания: каждые 3 пропуска — ещё на базовый интервал (максимум ×3).</summary>
    public static TimeSpan EffectiveInterval(AppSettings settings, int consecutiveIgnored)
    {
        var baseMinutes = Math.Max(5, settings.ReminderIntervalMinutes);
        var factor = 1 + Math.Min(2, consecutiveIgnored / 3);
        return TimeSpan.FromMinutes(baseMinutes * factor);
    }

    public static bool IsQuietHour(int hour, int from, int to)
    {
        if (from == to)
        {
            return false;
        }

        return from < to ? hour >= from && hour < to : hour >= from || hour < to;
    }

    public static int CountToday(AppSettings settings, DateOnly today) =>
        settings.ReminderCountDay == today ? settings.ReminderCountToday : 0;

    /// <summary>Фиксирует показ: увеличивает суточный счётчик и запоминает время.</summary>
    public static void RegisterShown(AppSettings settings, DateTime nowUtc, DateOnly today)
    {
        settings.ReminderCountToday = CountToday(settings, today) + 1;
        settings.ReminderCountDay = today;
        settings.LastReminderAt = nowUtc;
    }

    /// <summary>«Позже»: показ не считается, следующее напоминание через snooze.</summary>
    public static void Snooze(AppSettings settings, DateTime nowUtc, DateOnly today, TimeSpan snooze)
    {
        settings.ReminderCountToday = Math.Max(0, CountToday(settings, today) - 1);
        settings.ReminderCountDay = today;
        settings.LastReminderAt = nowUtc + snooze - EffectiveInterval(settings, 0);
    }
}
