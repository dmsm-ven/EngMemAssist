using System.Runtime.InteropServices;
using System.Text;

namespace EngMemAssist.Infrastructure;

/// <summary>
/// Определяет, можно ли сейчас показывать всплывающие окна: нет ли полноэкранного приложения
/// (игра, видео, презентация), не включён ли «Не беспокоить», и давно ли пользователь что-то нажимал.
/// </summary>
public static class UserActivityMonitor
{
    /// <summary>true — сейчас лучше не беспокоить.</summary>
    public static bool IsNotificationBlocked()
    {
        try
        {
            // Системный ответ: полноэкранная игра (D3D), презентация, «Не беспокоить», заставка/блокировка.
            if (SHQueryUserNotificationState(out var state) == 0 && state != QunsAcceptsNotifications)
            {
                return true;
            }

            // Запасной вариант: окно без рамки, закрывающее весь монитор (видео в браузере по F11, borderless-игры).
            return IsForegroundWindowFullscreen();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Сколько времени пользователь не трогал мышь и клавиатуру.</summary>
    public static TimeSpan GetIdleTime()
    {
        try
        {
            var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
            if (!GetLastInputInfo(ref info))
            {
                return TimeSpan.Zero;
            }

            var idle = unchecked((uint)Environment.TickCount - info.Time);
            return TimeSpan.FromMilliseconds(idle);
        }
        catch (Exception)
        {
            return TimeSpan.Zero;
        }
    }

    private static bool IsForegroundWindowFullscreen()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId)
        {
            return false;
        }

        var className = new StringBuilder(64);
        GetClassName(window, className, className.Capacity);
        if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd")
        {
            return false; // рабочий стол и панель задач
        }

        if (!GetWindowRect(window, out var rect))
        {
            return false;
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(window, MonitorDefaultToNearest), ref info))
        {
            return false;
        }

        var coversMonitor = rect.Left <= info.Monitor.Left && rect.Top <= info.Monitor.Top
            && rect.Right >= info.Monitor.Right && rect.Bottom >= info.Monitor.Bottom;

        // Развёрнутое обычное окно при автоскрытии панели задач тоже занимает весь монитор, но у него есть заголовок.
        var style = GetWindowLong(window, GwlStyle);
        var hasCaption = (style & WsCaption) == WsCaption;

        return coversMonitor && !hasCaption;
    }

    private const int QunsAcceptsNotifications = 5;
    private const int GwlStyle = -16;
    private const int WsCaption = 0x00C00000;
    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
