namespace EngMemAssist.Core.Settings;

public sealed class AppSettings
{
    /// <summary>Ссылка на Google Spreadsheet. Пусто — при старте покажем окно подключения.</summary>
    public string? SpreadsheetUrl { get; set; }

    /// <summary>Когда последний раз успешно завершилась синхронизация со таблицей (UTC). При старте не синхронизируемся, если прошло меньше 30 минут.</summary>
    public DateTime? LastSyncAt { get; set; }

    public string Theme { get; set; } = "Dark";

    /// <summary>Имя файла фонового изображения в папке приложения (null — фона нет).</summary>
    public string? BackgroundImageFile { get; set; }

    /// <summary>Замостить окно картинкой (для небольших бесшовных текстур) вместо растяжения.</summary>
    public bool BackgroundTile { get; set; }

    /// <summary>Масштаб плитки при замощении: 1 — исходный размер, 0.5 — вдвое меньше.</summary>
    public double BackgroundTileScale { get; set; } = 1.0;

    /// <summary>Непрозрачность изображения поверх цвета подложки, 0..1.</summary>
    public double BackgroundOpacity { get; set; } = 0.4;

    /// <summary>Цвет подложки под изображением, #RRGGBB.</summary>
    public string BackgroundOverlayColor { get; set; } = "#000000";

    /// <summary>Локальные текстовые базы предложений: ищутся раньше внешнего API.</summary>
    public List<LocalSource> LocalSources { get; set; } = [];

    /// <summary>Стратегия выбора слов для квиза (выбирается на главной).</summary>
    public Models.QuizStrategy QuizStrategy { get; set; } = Models.QuizStrategy.Normal;

    /// <summary>Звуковые эффекты в квизе.</summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>Встряска страницы при неверном ответе.</summary>
    public bool ShakeOnWrong { get; set; } = true;

    public int QuizQuestionCount { get; set; } = 10;
    public int QuizOptionsCount { get; set; } = 8;
    public int SentencesPerWord { get; set; } = 4;
}
