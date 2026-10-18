using Microsoft.Win32;

namespace EngMemAssist.Infrastructure;

/// <summary>Запуск вместе с Windows через раздел реестра текущего пользователя (права администратора не нужны).</summary>
public static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EngMemAssist";

    /// <summary>Аргумент запуска: при включённых напоминаниях окно не показывается, приложение сидит в трее.</summary>
    public const string MinimizedArgument = "--minimized";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue(ValueName, Command());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>Если автозапуск включён, а exe переехал — обновляет путь.</summary>
    public static void RefreshPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(ValueName) is string current && current != Command())
        {
            key.SetValue(ValueName, Command());
        }
    }

    private static string Command() => $"\"{Environment.ProcessPath}\" {MinimizedArgument}";
}
