using System.Text.Json;

namespace EngMemAssist.Core.Settings;

/// <summary>Хранит настройки в settings.json. Битый или отсутствующий файл — настройки по умолчанию.</summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonSettingsService(string filePath)
    {
        _filePath = filePath;
        Current = Load();
    }

    public AppSettings Current { get; }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Current, JsonOptions));
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath)) ?? new AppSettings();
            }
        }
        catch (JsonException)
        {
        }

        return new AppSettings();
    }
}
