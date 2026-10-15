namespace EngMemAssist.Core.Models;

public sealed class StatsOverview
{
    public int SessionsCount { get; set; }
    public int AnswersCount { get; set; }
    public int CorrectAnswers { get; set; }
    public int ActiveWords { get; set; }

    public double CorrectPercent => AnswersCount == 0 ? 0 : 100.0 * CorrectAnswers / AnswersCount;
}

public sealed class SessionSummary
{
    public long Id { get; set; }
    public DateTime StartedAt { get; set; }
    public int QuestionCount { get; set; }
    public int CorrectCount { get; set; }

    /// <summary>Сколько подсказок взято в этом тесте.</summary>
    public int HintsUsed { get; set; }

    public string StartedAtLocal => DateTime.SpecifyKind(StartedAt, DateTimeKind.Utc).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string ScoreText => $"{CorrectCount} / {QuestionCount}";
}

public sealed class WordDifficulty
{
    public string English { get; set; } = "";
    public string Translation { get; set; } = "";
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }

    public string ErrorPercentText =>
        CorrectCount + WrongCount == 0 ? "—" : $"{100.0 * WrongCount / (CorrectCount + WrongCount):0}%";
}
