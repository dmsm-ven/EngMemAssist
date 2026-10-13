namespace EngMemAssist.Core.Models;

public sealed class QuizSession
{
    public long Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int QuestionCount { get; set; }
}
