namespace EngMemAssist.Core.Models;

public sealed record QuizResultRow(
    int Number,
    string English,
    string Chosen,
    string CorrectOption,
    string AllTranslations,
    bool IsCorrect)
{
    public string ResultMark => IsCorrect ? "✓" : "✗";
}

public sealed record QuizResult(IReadOnlyList<QuizResultRow> Rows)
{
    public int Total => Rows.Count;

    public int CorrectCount => Rows.Count(r => r.IsCorrect);
}
