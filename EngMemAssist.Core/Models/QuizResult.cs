namespace EngMemAssist.Core.Models;

public sealed record QuizResultRow(
    int Number,
    string English,
    string Chosen,
    string CorrectOption,
    string AllTranslations,
    bool IsCorrect,
    QuizHint Hint = QuizHint.None)
{
    public string ResultMark => IsCorrect ? "✓" : "✗";

    /// <summary>Значок взятой подсказки (пусто, если подсказка не использовалась).</summary>
    public string HintIcon => Hint.Icon();

    public string HintDescription => Hint.Description();
}

public sealed record QuizResult(IReadOnlyList<QuizResultRow> Rows)
{
    public int Total => Rows.Count;

    public int CorrectCount => Rows.Count(r => r.IsCorrect);

    public int HintsUsed => Rows.Count(r => r.Hint != QuizHint.None);
}
