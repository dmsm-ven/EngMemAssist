using EngMemAssist.Core.Abstractions;

namespace EngMemAssist.Core.Services;

/// <summary>
/// Слово или фраза из таблицы в разобранном виде: формы и нормальные формы каждого слова.
/// Используется и для ключа дубликатов (Lemma), и для поиска слова в предложении.
/// </summary>
public sealed record WordPattern(IReadOnlyList<string> Forms, IReadOnlyList<string> Lemmas)
{
    public bool IsEmpty => Lemmas.Count == 0;

    /// <summary>Нормальная форма целиком: для child — "child", для give up — "give up".</summary>
    public string Lemma => string.Join(' ', Lemmas);

    public static WordPattern Create(ILemmatizer lemmatizer, string english)
    {
        var tokens = lemmatizer.Analyze(english);
        return new WordPattern(
            tokens.Select(t => t.Text.ToLowerInvariant()).ToList(),
            tokens.Select(t => t.Lemma.ToLowerInvariant()).ToList());
    }
}
