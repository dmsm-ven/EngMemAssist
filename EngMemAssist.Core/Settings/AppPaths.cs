namespace EngMemAssist.Core.Settings;

/// <summary>Расположение файлов приложения в %AppData%/EngMemAssist.</summary>
public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EngMemAssist");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string DatabaseFile => Path.Combine(Root, "engmemassist.db");

    /// <summary>OAuth client secret из Google Cloud (Desktop app). Кладётся пользователем вручную.</summary>
    public static string ClientSecretFile => Path.Combine(Root, "client_secret.json");

    /// <summary>Папка, где Google.Apis хранит OAuth-токены.</summary>
    public static string GoogleTokenDirectory => Path.Combine(Root, "google-token");

    public static void EnsureCreated() => Directory.CreateDirectory(Root);
}
