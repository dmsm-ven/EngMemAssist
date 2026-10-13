namespace EngMemAssist.Core.Models;

public sealed class QuizAnswer
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long WordId { get; set; }
    public long? SentenceId { get; set; }
    public string ChosenTranslation { get; set; } = "";
    public string CorrectTranslation { get; set; } = "";
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; }
}
