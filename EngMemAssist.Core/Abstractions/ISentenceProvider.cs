namespace EngMemAssist.Core.Abstractions;

public sealed record SentenceCandidate(string Text, string? Translation, string Source);

/// <summary>Что искать: слово как записано в таблице и его нормальная форма.</summary>
public sealed record SentenceQuery(string Word, string Lemma);

/// <summary>Поставщик реальных предложений, содержащих слово (Tatoeba, Wiktionary).</summary>
public interface ISentenceProvider
{
    /// <summary>
    /// Ищет предложения по слову. Возвращает кандидатов без проверки формы слова:
    /// точное совпадение и позицию определяет вызывающий код.
    /// </summary>
    /// <exception cref="Exceptions.SentenceProviderException">Сервис недоступен.</exception>
    /// <exception cref="Exceptions.TooManyRequestsException">Превышен лимит запросов.</exception>
    Task<IReadOnlyList<SentenceCandidate>> FindAsync(SentenceQuery query, int maxResults, CancellationToken cancellationToken = default);
}
