using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;

namespace EngMemAssist.Integrations;

/// <summary>Реальные предложения с переводами из корпуса Tatoeba (бесплатно, без ключа).</summary>
public sealed class TatoebaSentenceProvider : ISentenceProvider
{
    private const string BaseUrl = "https://tatoeba.org/en/api_v0/search";
    private const int MaxPages = 2;

    private readonly HttpClient _http;

    public TatoebaSentenceProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<SentenceCandidate>> FindAsync(
        string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var result = new List<SentenceCandidate>();

        for (var page = 1; page <= MaxPages && result.Count < maxResults; page++)
        {
            var batch = await FetchPageAsync(query, page, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            result.AddRange(batch);
        }

        return result;
    }

    private async Task<List<SentenceCandidate>> FetchPageAsync(string query, int page, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}?from=eng&to=rus&query={Uri.EscapeDataString(query)}" +
                  $"&trans_filter=limit&orphans=no&unapproved=no&sort=random&page={page}";

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
