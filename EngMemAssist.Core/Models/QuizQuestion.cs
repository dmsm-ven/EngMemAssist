namespace EngMemAssist.Core.Models;

/// <summary>Предложение со словом, выделенным в тексте (для подсказки «ещё одно предложение»).</summary>
public sealed record QuizSentence(string Text, int MatchStart, int MatchLength, string? SourceText = null)
{
    public string Before => Slice(Text, 0, MatchStart);

    public string Highlighted => Slice(Text, MatchStart, MatchLength);

    public string After => Slice(Text, MatchStart + MatchLength, Text.Length - MatchStart - MatchLength);

    internal static string Slice(string text, int start, int length) =>
        start < 0 || length < 0 || start + length > text.Length
            ? (start == 0 ? text : "")
            : text.Substring(start, length);
}

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
    string AllTranslations,
    IReadOnlyList<QuizSentence>? ExtraSentences = null,
    string? SourceText = null)
{
    public string Before => QuizSentence.Slice(SentenceText, 0, MatchStart);

    public string Highlighted => QuizSentence.Slice(SentenceText, MatchStart, MatchLength);

    public string After => QuizSentence.Slice(SentenceText, MatchStart + MatchLength, SentenceText.Length - MatchStart - MatchLength);

    /// <summary>Другие предложения этого слова (для подсказки). Может быть пусто.</summary>
    public IReadOnlyList<QuizSentence> ExtraSentenceList => ExtraSentences ?? [];

    /// <summary>Верным считается любое из значений перевода слова, а не только показанное в вариантах.</summary>
    public bool IsCorrect(string? chosen) =>
        chosen is not null && AllTranslations
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(t => string.Equals(t, chosen, StringComparison.OrdinalIgnoreCase));
}
