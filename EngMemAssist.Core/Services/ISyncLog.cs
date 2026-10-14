using EngMemAssist.Core.Settings;

namespace EngMemAssist.Core.Services;

/// <summary>Журнал синхронизации для диагностики: что нашлось по каждому слову.</summary>
public interface ISyncLog
{
    void Write(string message);
}

/// <summary>Пишет журнал в sync.log (%AppData%/EngMemAssist). Ошибки записи игнорируются; файл обнуляется после 512 КБ.</summary>
public sealed class FileSyncLog : ISyncLog
{
    private const long MaxBytes = 512 * 1024;
    private readonly object _sync = new();
    private readonly string _path;

    public FileSyncLog(string? path = null)
    {
        _path = path ?? AppPaths.SyncLogFile;
    }

    public void Write(string message)
    {
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                if (File.Exists(_path) && new FileInfo(_path).Length > MaxBytes)
                {
                    File.Delete(_path);
                }

                File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Журнал не должен ломать синхронизацию.
        }
    }
}
