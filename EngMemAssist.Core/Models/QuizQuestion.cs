namespace EngMemAssist.Core.Models;

/// <summary>
/// Один вопрос квиза: английское предложение, в котором выделено слово, и варианты перевода этого слова.
/// </summary>
public sealed record QuizQuestion(
    long WordId,
    long SentenceId,
    string English,
    string SentenceText,
    int MatchStart,
    int MatchLength,
    IReadOnlyList<string> Options,
    string CorrectOption,
    string AllTranslations)
{
    public string Before => Slice(0, MatchStart);

    public string Highlighted => Slice(MatchStart, MatchLength);

    public string After => Slice(MatchStart + MatchLength, SentenceText.Length - MatchStart - MatchLength);

    /// <summary>Верным считается любое из значений перевода слова, а не только показанное в вариантах.</summary>
    public bool IsCorrect(string? chosen) =>
        chosen is not null && AllTranslations
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(t => string.Equals(t, chosen, StringComparison.OrdinalIgnoreCase));

    private string Slice(int start, int length) =>
        start < 0 || length < 0 || start + length > SentenceText.Length
            ? (start == 0 ? SentenceText : "")
            : SentenceText.Substring(start, length);
}
