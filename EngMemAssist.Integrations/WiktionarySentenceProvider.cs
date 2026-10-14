using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;

namespace EngMemAssist.Integrations;

/// <summary>
/// Примеры употребления из английского Викисловаря (бесплатно, без ключа). Хорошо покрывает редкие слова,
/// которых нет в Tatoeba, и даёт более длинный контекст (в том числе цитаты из книг). Переводов не даёт.
/// </summary>
public sealed partial class WiktionarySentenceProvider : ISentenceProvider
{
    private const string BaseUrl = "https://en.wiktionary.org/api/rest_v1/page/definition/";

    private readonly ThrottledHttpClient _http;

    public WiktionarySentenceProvider(ThrottledHttpClient http)
    {
        _http = http;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpacesRegex();

    public async Task<IReadOnlyList<SentenceCandidate>> FindAsync(
        SentenceQuery query, int maxResults, CancellationToken cancellationToken = default)
    {
        // Статьи в Викисловаре заведены на нормальную форму слова.
        var list = await LoadAsync(query.Lemma, cancellationToken);

        if (list.Count == 0 && !string.Equals(query.Lemma, query.Word, StringComparison.OrdinalIgnoreCase))
        {
            list = await LoadAsync(query.Word, cancellationToken);
        }

        return list.Take(maxResults).ToList();
    }

    private async Task<List<SentenceCandidate>> LoadAsync(string word, CancellationToken cancellationToken)
    {
        var url = BaseUrl + Uri.EscapeDataString(word.Trim().ToLowerInvariant());

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new TooManyRequestsException("Викисловарь: слишком много запросов (Too Many Requests). Попробуйте позже.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new SentenceProviderException($"Викисловарь вернул ошибку {(int)response.StatusCode}.");
            }

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            return Parse(json.RootElement);
        }
        catch (HttpRequestException ex)
        {
            throw new SentenceProviderException("Викисловарь недоступен. Проверьте интернет.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SentenceProviderException("Викисловарь не ответил вовремя.", ex);
        }
        catch (JsonException ex)
        {
            throw new SentenceProviderException("Викисловарь вернул ответ в неожиданном формате.", ex);
        }
    }

    /// <summary>Ответ: { "en": [ { definitions: [ { examples: [...], parsedExamples: [ { example } ] } ] } ], "fr": [...] }.</summary>
    private static List<SentenceCandidate> Parse(JsonElement root)
    {
        var list = new List<SentenceCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("en", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("definitions", out var definitions) || definitions.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var definition in definitions.EnumerateArray())
            {
                foreach (var raw in ReadExamples(definition))
                {
                    var text = Clean(raw);
                    if (text.Length > 0 && seen.Add(text))
                    {
                        list.Add(new SentenceCandidate(text, null, "wiktionary"));
                    }
                }
            }
        }

        return list;
    }

    private static IEnumerable<string> ReadExamples(JsonElement definition)
    {
        if (definition.TryGetProperty("parsedExamples", out var parsed) && parsed.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in parsed.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("example", out var example) && example.GetString() is { } value)
                {
                    yield return value;
                }
            }

            yield break;
        }

        if (definition.TryGetProperty("examples", out var examples) && examples.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in examples.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } value)
                {
                    yield return value;
                }
            }
        }
    }

    private static string Clean(string html)
    {
        var text = WebUtility.HtmlDecode(TagRegex().Replace(html, ""));
        return SpacesRegex().Replace(text, " ").Trim();
    }
}
