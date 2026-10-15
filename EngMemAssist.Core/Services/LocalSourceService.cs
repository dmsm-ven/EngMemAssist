using EngMemAssist.Core.Settings;

namespace EngMemAssist.Core.Services;

public interface ILocalSourceService
{
    IReadOnlyList<LocalSource> Sources { get; }

    /// <summary>Копирует файл в папку приложения, считает размер и строки и запоминает в настройках.</summary>
    Task<LocalSource> AddAsync(string path, CancellationToken cancellationToken = default);

    void Remove(string id);

    /// <summary>Сводка по всем источникам: «3 файла(ов) | 14 Mb | 99999 предложений».</summary>
    string BuildSummary();
}

public sealed class LocalSourceService : ILocalSourceService
{
    private readonly ISettingsService _settings;

    public LocalSourceService(ISettingsService settings)
    {
        _settings = settings;
    }

    public IReadOnlyList<LocalSource> Sources => _settings.Current.LocalSources;

    public async Task<LocalSource> AddAsync(string path, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(AppPaths.SourcesDirectory);

        var source = new LocalSource
        {
            DisplayName = Path.GetFileNameWithoutExtension(path),
        };
        source.FileName = source.Id + ".txt";

        await Task.Run(() =>
        {
            File.Copy(path, source.FullPath, overwrite: true);
            source.SizeBytes = new FileInfo(source.FullPath).Length;
            source.LineCount = File.ReadLines(source.FullPath).Count(l => !string.IsNullOrWhiteSpace(l));
        }, cancellationToken);

        _settings.Current.LocalSources.Add(source);
        _settings.Save();
        return source;
    }

    public void Remove(string id)
    {
        var source = _settings.Current.LocalSources.FirstOrDefault(s => s.Id == id);
        if (source is null)
        {
            return;
        }

        _settings.Current.LocalSources.Remove(source);
        _settings.Save();

        try
        {
            if (File.Exists(source.FullPath))
            {
                File.Delete(source.FullPath);
            }
        }
        catch (IOException)
        {
        }
    }

    public string BuildSummary()
    {
        var all = _settings.Current.LocalSources;
        return $"{all.Count} файл(ов) | {LocalSource.FormatSize(all.Sum(s => s.SizeBytes))} | {all.Sum(s => (long)s.LineCount)} предложений";
    }
}
