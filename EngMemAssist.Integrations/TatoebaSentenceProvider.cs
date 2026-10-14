using System.Net;
using System.Text.Json;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;

namespace EngMemAssist.Integrations;

/// <summary>
/// Реальные предложения с русскими переводами из корпуса Tatoeba (бесплатно, без ключа).
/// Tatoeba сам учитывает формы слова (по stirs найдёт stir, stirred, stirring), но корпус состоит
/// в основном из коротких фраз, поэтому запрашиваются сначала самые длинные предложения, затем случайная выборка.
/// </summary>
public sealed class TatoebaSentenceProvider : ISentenceProvider
{
    private const string BaseUrl = "https://tatoeba.org/en/api_v0/search";

    // Страница даёт 10 предложений. Первый порядок — по числу слов, от длинных к коротким.
    private static readonly string[] Orders = ["sort=words&sort_reverse=yes", "sort=random"];

    private readonly ThrottledHttpClient _http;

    public TatoebaSentenceProvider(ThrottledHttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<SentenceCandidate>> FindAsync(
        SentenceQuery query, int maxResults, CancellationToken cancellationToken = default)
    {
        var result = await SearchAsync(query.Word, cancellationToken);

        if (result.Count == 0 && !string.Equals(query.Lemma, query.Word, StringComparison.OrdinalIgnoreCase))
        {
            result = await SearchAsync(query.Lemma, cancellationToken);
        }

        return result.Take(maxResults).ToList();
    }

    private async Task<List<SentenceCandidate>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        var list = new List<SentenceCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var order in Orders)
        {
            foreach (var candidate in await FetchPageAsync(text, order, cancellationToken))
            {
                if (seen.Add(candidate.Text))
                {
                    list.Add(candidate);
                }
            }

            // Если по слову ничего нет, вторую выборку не запрашиваем.
            if (list.Count == 0)
            {
                break;
            }
        }

        return list;
    }

    private async Task<List<SentenceCandidate>> FetchPageAsync(string text, string order, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}?from=eng&to=rus&query={Uri.EscapeDataString(text)}" +
                  $"&trans_filter=limit&orphans=no&unapproved=no&{order}&page=1";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new TooManyRequestsException("Tatoeba: слишком много запросов (Too Many Requests). Попробуйте позже.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new SentenceProviderException($"Tatoeba вернул ошибку {(int)response.StatusCode}.");
            }

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            return Parse(json.RootElement);
        }
        catch (HttpRequestException ex)
        {
            throw new SentenceProviderException("Tatoeba недоступен. Проверьте интернет.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SentenceProviderException("Tatoeba не ответил вовремя.", ex);
        }
        catch (JsonException ex)
        {
            throw new SentenceProviderException("Tatoeba вернул ответ в неожиданном формате.", ex);
        }
    }

    private static List<SentenceCandidate> Parse(JsonElement root)
    {
        var list = new List<SentenceCandidate>();

        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var item in results.EnumerateArray())
        {
            if (!item.TryGetProperty("text", out var textElement) || textElement.GetString() is not { Length: > 0 } text)
            {
                continue;
            }

            list.Add(new SentenceCandidate(text, FindRussianTranslation(item), "tatoeba"));
        }

        return list;
    }

    /// <summary>translations — массив массивов (прямые и косвенные переводы); берём первый русский.</summary>
    private static string? FindRussianTranslation(JsonElement sentence)
    {
        if (!sentence.TryGetProperty("translations", out var groups) || groups.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var group in groups.EnumerateArray())
        {
            if (group.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var translation in group.EnumerateArray())
            {
                if (translation.ValueKind == JsonValueKind.Object
                    && translation.TryGetProperty("lang", out var lang) && lang.GetString() == "rus"
                    && translation.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}
