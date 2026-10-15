namespace EngMemAssist.Core.Settings;

public sealed class AppSettings
{
    /// <summary>Ссылка на Google Spreadsheet. Пусто — при старте покажем окно подключения.</summary>
    public string? SpreadsheetUrl { get; set; }

    public string Theme { get; set; } = "Dark";

    /// <summary>Имя файла фонового изображения в папке приложения (null — фона нет).</summary>
    public string? BackgroundImageFile { get; set; }

    /// <summary>Замостить окно картинкой (для небольших бесшовных текстур) вместо растяжения.</summary>
    public bool BackgroundTile { get; set; }

    /// <summary>Непрозрачность изображения поверх цвета подложки, 0..1.</summary>
    public double BackgroundOpacity { get; set; } = 0.4;

    /// <summary>Цвет подложки под изображением, #RRGGBB.</summary>
    public string BackgroundOverlayColor { get; set; } = "#000000";

    public int QuizQuestionCount { get; set; } = 10;
    public int QuizOptionsCount { get; set; } = 8;
    public int SentencesPerWord { get; set; } = 4;
}
