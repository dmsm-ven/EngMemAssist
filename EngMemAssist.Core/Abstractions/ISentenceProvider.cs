namespace EngMemAssist.Core.Abstractions;

public sealed record SentenceCandidate(string Text, string? Translation, string Source);

/// <summary>Поставщик реальных предложений, содержащих слово (сейчас Tatoeba).</summary>
public interface ISentenceProvider
{
    Task<IReadOnlyList<SentenceCandidate>> FindAsync(string word, int count, CancellationToken cancellationToken = default);
}
