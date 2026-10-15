using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Settings;

namespace EngMemAssist.Core.Services;

/// <summary>
/// Ищет предложения со словами в локальных .txt-базах. Файлы читаются за один проход для всех слов сразу;
/// возвращаются все вхождения (но не больше <see cref="MaxPerWord"/> на слово).
/// </summary>
public sealed class LocalSentenceSearcher
{
    public const int MaxPerWord = 2000;

    private const int MinWordsInSentence = 4;
    private const int MaxWordsInSentence = 40;
    private const int MinChars = 15;
    private const int MaxChars = 400;

    private readonly ISettingsService _settings;
    private readonly ILemmatizer _lemmatizer;
    private readonly ISyncLog _log;

    public LocalSentenceSearcher(ISettingsService settings, ILemmatizer lemmatizer, ISyncLog log)
    {
        _settings = settings;
        _lemmatizer = lemmatizer;
        _log = log;
    }

    public bool HasSources => _settings.Current.LocalSources.Count > 0;

    public Task<Dictionary<long, List<Sentence>>> FindAsync(
        IReadOnlyList<Word> words, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var sources = _settings.Current.LocalSources.ToList();
        return Task.Run(() => Find(words, sources, progress, cancellationToken), cancellationToken);
    }

    private sealed class Entry
    {
        public required Word Word { get; init; }
        public required WordPattern Pattern { get; init; }

        /// <summary>Для каждого слова фразы — подстроки, одна из которых должна встретиться в строке (быстрый предфильтр).</summary>
        public required string[][] Alternatives { get; init; }

        public List<Sentence> Hits { get; } = [];
        public HashSet<string> Seen { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<long, List<Sentence>> Find(
        IReadOnlyList<Word> words, List<LocalSource> sources, IProgress<string>? progress, CancellationToken ct)
    {
        var entries = new List<Entry>();
        foreach (var word in words)
        {
            var pattern = WordPattern.Create(_lemmatizer, word.English);
            if (pattern.IsEmpty)
            {
                continue;
            }

            var alternatives = pattern.Lemmas
                .Select((lemma, i) => new[] { Prefix(lemma), Prefix(i < pattern.Forms.Count ? pattern.Forms[i] : lemma) }
                    .Distinct().ToArray())
                .ToArray();

            entries.Add(new Entry { Word = word, Pattern = pattern, Alternatives = alternatives });
        }

        foreach (var source in sources)
        {
            if (!File.Exists(source.FullPath))
            {
                _log.Write($"Локальный источник «{source.DisplayName}»: файл не найден, пропущен.");
                continue;
            }

            var before = entries.Sum(e => e.Hits.Count);
            var lineNumber = 0;

            foreach (var raw in File.ReadLines(source.FullPath))
            {
                ct.ThrowIfCancellationRequested();

                if (++lineNumber % 20000 == 0)
                {
                    progress?.Report($"Поиск в «{source.DisplayName}»: обработано {lineNumber} из {source.LineCount} строк");
                }

                var line = raw.Trim();
                if (line.Length < MinChars || line.Length > MaxChars)
                {
                    continue;
                }

                List<LemmaToken>? tokens = null;
                string? text = null;

                foreach (var entry in entries)
                {
                    if (entry.Hits.Count >= MaxPerWord || !PassesPrefilter(line, entry))
                    {
                        continue;
                    }

                    if (tokens is null)
                    {
                        text = string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                        tokens = _lemmatizer.Analyze(text).ToList();
                    }

                    if (tokens.Count < MinWordsInSentence || tokens.Count > MaxWordsInSentence)
                    {
                        break;
                    }

                    if (!SentenceMatcher.TryFind(tokens, entry.Pattern, out var start, out var length) ||
                        !entry.Seen.Add(text!))
                    {
                        continue;
                    }

                    entry.Hits.Add(new Sentence
                    {
                        WordId = entry.Word.Id,
                        Text = text!,
                        Source = "local:" + source.DisplayName,
                        MatchStart = start,
                        MatchLength = length,
                    });
                }
            }

            _log.Write($"Локальный источник «{source.DisplayName}»: найдено вхождений {entries.Sum(e => e.Hits.Count) - before}.");
        }

        return entries.Where(e => e.Hits.Count > 0).ToDictionary(e => e.Word.Id, e => e.Hits);
    }

    private static bool PassesPrefilter(string line, Entry entry)
    {
        foreach (var alternatives in entry.Alternatives)
        {
            var found = false;
            foreach (var part in alternatives)
            {
                if (line.Contains(part, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Основа слова: отрезаем до двух последних букв, чтобы поймать формы (-s, -ed, -ing). Нерегулярные формы ловит только по совпадению основы.</summary>
    private static string Prefix(string word) => word.Length <= 4 ? word : word[..Math.Max(4, word.Length - 2)];
}
