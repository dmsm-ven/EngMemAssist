using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;

namespace EngMemAssist.Integrations;

/// <summary>
/// Объединяет несколько источников предложений. Если один источник недоступен, берутся результаты остальных.
/// Если какой-то источник ответил «Too Many Requests», об этом сообщается вызывающему коду.
/// </summary>
public sealed class CompositeSentenceProvider : ISentenceProvider
{
    private readonly IReadOnlyList<ISentenceProvider> _providers;

    public CompositeSentenceProvider(IReadOnlyList<ISentenceProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<SentenceCandidate>> FindAsync(
        SentenceQuery query, int maxResults, CancellationToken cancellationToken = default)
    {
        var all = new List<SentenceCandidate>();
        TooManyRequestsException? rateLimit = null;
        SentenceProviderException? firstError = null;
        var succeeded = 0;

        foreach (var provider in _providers)
        {
            try
            {
                all.AddRange(await provider.FindAsync(query, maxResults, cancellationToken));
                succeeded++;
            }
            catch (TooManyRequestsException ex)
            {
                rateLimit ??= ex;
            }
            catch (SentenceProviderException ex)
            {
                firstError ??= ex;
            }
        }

        if (rateLimit is not null)
        {
            throw rateLimit;
        }

        if (succeeded == 0 && firstError is not null)
        {
            throw firstError;
        }

        return all;
    }
}
