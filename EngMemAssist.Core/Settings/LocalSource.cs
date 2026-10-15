using System.Globalization;
using System.Text.Json.Serialization;

namespace EngMemAssist.Core.Settings;

/// <summary>Подключённый пользователем .txt-файл с предложениями (одна строка — одно предложение).</summary>
public sealed class LocalSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Имя для списка: имя исходного файла без расширения.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Имя копии файла в папке sources.</summary>
    public string FileName { get; set; } = "";

    public long SizeBytes { get; set; }

    /// <summary>Число непустых строк (предложений).</summary>
    public int LineCount { get; set; }

    [JsonIgnore]
    public string FullPath => Path.Combine(AppPaths.SourcesDirectory, FileName);

    [JsonIgnore]
    public string SizeText => FormatSize(SizeBytes);

    [JsonIgnore]
    public string LinesText => $"{LineCount} строк";

    public static string FormatSize(long bytes)
    {
        var culture = CultureInfo.InvariantCulture;
        return bytes switch
        {
            >= 1024L * 1024 => (bytes / 1024.0 / 1024.0).ToString("0.#", culture) + " Mb",
            _ => Math.Max(1, (long)Math.Round(bytes / 1024.0)).ToString(culture) + " Kb",
        };
    }
}
