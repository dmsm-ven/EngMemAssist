using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Settings;

namespace EngMemAssist.Core.Services;

public sealed class WordSyncService : IWordSyncService
{
    private const int MinWordsInSentence = 4;
    private const int MaxWordsInSentence = 30;
    private const int MaxCandidatesPerWord = 60;

    private readonly IWordSource _source;
    private readonly ILemmatizer _lemmatizer;
    private readonly IWordRepository _words;
    private readonly ISentenceRepository _sentences;
    private readonly ISentenceProvider _provider;
    private readonly ISettingsService _settings;

    public WordSyncService(
        IWordSource source,
        ILemmatizer lemmatizer,
        IWordRepository words,
        ISentenceRepository sentences,
        ISentenceProvider provider,
        ISettingsService settings)
    {
        _source = source;
        _lemmatizer = lemmatizer;
        _words = words;
        _sentences = sentences;
        _provider = provider;
        _settings = settings;
    }

    public async Task<SyncResult> SyncAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("Загрузка списка слов из таблицы (при первом запуске откроется браузер для входа в Google)…");
        var rows = await _source.LoadAsync(cancellationToken);

        progress?.Report("Обработка слов (первый запуск может занять время: загружаются языковые модели)…");
        var parsed = await Task.Run(() => Normalize(rows), cancellationToken);

        var existing = await _words.GetAllAsync();
        var diff = await ApplyDiffAsync(parsed, existing);

        var toFetch = diff.Active.Where(w => w.SentencesFetchedAt is null).ToList();
        var sentencesAdded = 0;
        var failed = 0;
        var rateLimited = false;
        string? rateLimitMessage = null;

        var index = 0;
        foreach (var word in toFetch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            progress?.Report($"Загрузка предложений: {word.English} ({index} из {toFetch.Count})");

            try
            {
                var found = await FetchSentencesAsync(word, cancellationToken);
                await _sentences.AddRangeAsync(found);

                word.SentencesFetchedAt = DateTime.UtcNow;
                await _words.UpdateAsync(word);
                sentencesAdded += found.Count;
            }
            catch (TooManyRequestsException ex)
            {
                rateLimited = true;
                rateLimitMessage = ex.Message;
                break;
            }
            catch (SentenceProviderException)
            {
                // Слово останется без отметки и будет запрошено заново при следующей синхронизации.
                failed++;
            }
        }

        return new SyncResult(
            diff.Added,
            diff.Updated,
            diff.Removed,
            sentencesAdded,
            failed,
            diff.Active.Count,
            rateLimited,
            rateLimitMessage);
    }

    private sealed record ParsedWord(string English, string Translation, string Lemma);

    private sealed record DiffResult(int Added, int Updated, int Removed, List<Word> Active);

    /// <summary>Приводит строки таблицы к нормальной форме. Пустые строки и дубли по лемме отбрасываются.</summary>
    private List<ParsedWord> Normalize(IReadOnlyList<SourceWord> rows)
    {
        var result = new List<ParsedWord>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var english = CollapseSpaces(row.English);
            var translation = CollapseSpaces(row.Translation);
            if (english.Length == 0 || translation.Length == 0)
            {
                continue;
            }

            var pattern = WordPattern.Create(_lemmatizer, english);
            if (pattern.IsEmpty)
            {
                continue;
            }

            var lemma = pattern.Lemma;
            if (seen.Add(lemma))
            {
                result.Add(new ParsedWord(english, translation, lemma));
            }
        }

        return result;
    }

    private async Task<DiffResult> ApplyDiffAsync(IReadOnlyList<ParsedWord> parsed, IReadOnlyList<Word> existing)
    {
        var byLemma = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
        var byEnglish = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in existing)
        {
            byLemma.TryAdd(word.Lemma, word);
            byEnglish.TryAdd(word.English, word);
        }

        var matched = new HashSet<long>();
        var active = new List<Word>();
        int added = 0, updated = 0, removed = 0;

        foreach (var item in parsed)
        {
            // Поиск по английскому слову нужен, если сменился лемматизатор и леммы в базе устарели.
            if (!byLemma.TryGetValue(item.Lemma, out var word) && !byEnglish.TryGetValue(item.English, out word))
            {
                word = new Word
                {
                    Lemma = item.Lemma,
                    English = item.English,
                    Translation = item.Translation,
                    AddedAt = DateTime.UtcNow,
                };
                await _words.InsertAsync(word);
                added++;
                active.Add(word);
                continue;
            }

            matched.Add(word.Id);

            var changed = word.IsRemoved
                          || word.English != item.English
                          || word.Translation != item.Translation
                          || !string.Equals(word.Lemma, item.Lemma, StringComparison.OrdinalIgnoreCase);
            if (changed)
            {
                word.English = item.English;
                word.Translation = item.Translation;
                word.Lemma = item.Lemma;
                word.IsRemoved = false;
                await _words.UpdateAsync(word);
                updated++;
            }

            active.Add(word);
        }

        foreach (var word in existing)
        {
            if (!word.IsRemoved && !matched.Contains(word.Id))
            {
                word.IsRemoved = true;
                await _words.UpdateAsync(word);
                removed++;
            }
        }

        return new DiffResult(added, updated, removed, active);
    }

    private async Task<List<Sentence>> FetchSentencesAsync(Word word, CancellationToken cancellationToken)
    {
        var wanted = Math.Max(1, _settings.Current.SentencesPerWord);

        var queries = new List<string> { word.English };
        var lemmaQuery = word.Lemma;
        if (!lemmaQuery.Contains(' ') && !string.Equals(lemmaQuery, word.English, StringComparison.OrdinalIgnoreCase))
        {
            queries.Add(lemmaQuery);
        }

        var candidates = new List<SentenceCandidate>();
        var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in queries)
        {
            var batch = await _provider.FindAsync(query, MaxCandidatesPerWord, cancellationToken);
            foreach (var candidate in batch)
            {
                if (seenTexts.Add(candidate.Text.Trim()))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return await Task.Run(() => PickMatching(word, candidates, wanted), cancellationToken);
    }

    private List<Sentence> PickMatching(Word word, List<SentenceCandidate> candidates, int wanted)
    {
        var pattern = WordPattern.Create(_lemmatizer, word.English);
        var matches = new List<Sentence>();

        foreach (var candidate in candidates.Take(MaxCandidatesPerWord * 2))
        {
            var text = candidate.Text.Trim();
            var tokens = _lemmatizer.Analyze(text);
            if (tokens.Count < MinWordsInSentence || tokens.Count > MaxWordsInSentence)
            {
                continue;
            }

            if (!SentenceMatcher.TryFind(tokens, pattern, out var start, out var length))
            {
                continue;
            }

            matches.Add(new Sentence
            {
                WordId = word.Id,
                Text = text,
                Translation = candidate.Translation,
                Source = candidate.Source,
                MatchStart = start,
                MatchLength = length,
            });
        }

        return matches.OrderBy(_ => Random.Shared.Next()).Take(wanted).ToList();
    }

    private static string CollapseSpaces(string? value) =>
        string.Join(' ', (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
