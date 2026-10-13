namespace EngMemAssist.Core.Settings;

public sealed class AppSettings
{
    /// <summary>Ссылка на Google Spreadsheet. Пусто — при старте покажем окно подключения.</summary>
    public string? SpreadsheetUrl { get; set; }

    public string Theme { get; set; } = "Dark";

    public int QuizQuestionCount { get; set; } = 10;
    public int QuizOptionsCount { get; set; } = 8;
    public int SentencesPerWord { get; set; } = 4;
}
