using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Services;

public sealed class QuizService : IQuizService
{
    private readonly IWordRepository _words;
    private readonly ISentenceRepository _sentences;
    private readonly IQuizRepository _quizzes;

    public QuizService(IWordRepository words, ISentenceRepository sentences, IQuizRepository quizzes)
    {
        _words = words;
        _sentences = sentences;
        _quizzes = quizzes;
    }

    public async Task<IReadOnlyList<QuizQuestion>> CreateQuizAsync(
        int questionCount, int optionsCount, QuizStrategy strategy = QuizStrategy.Normal)
    {
        var words = await _words.GetActiveAsync();

        // Предложения целиком в память не грузим (с локальными базами их могут быть тысячи на слово):
        // сначала смотрим только, у каких слов они есть, а сами тексты читаем для выбранных слов.
        var counts = await _sentences.CountByWordAsync();

        var candidates = words
            .Where(w => w.GetTranslations().Count > 0 && counts.ContainsKey(w.Id))
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                "Нет слов для квиза: у слов пока нет проверочных предложений. Дождитесь окончания синхронизации.");
        }

        var chosenWords = PickWords(candidates, Math.Min(questionCount, candidates.Count), strategy);

        var sentences = await _sentences.GetByWordIdsAsync(chosenWords.Select(w => w.Id).ToList());
        var sentencesByWord = sentences
            .Where(s => s.HasValidMatch)
            .GroupBy(s => s.WordId)
            .ToDictionary(g => g.Key, g => g.ToList());
        chosenWords = chosenWords.Where(w => sentencesByWord.ContainsKey(w.Id)).ToList();

        if (chosenWords.Count == 0)
        {
            throw new InvalidOperationException(
                "Нет слов для квиза: у слов пока нет проверочных предложений. Дождитесь окончания синхронизации.");
        }

        var questions = new List<QuizQuestion>();

        foreach (var word in chosenWords)
        {
            var wordSentences = sentencesByWord[word.Id];
            var sentence = Pick(wordSentences);
            var extra = wordSentences
                .Where(s => s.Id != sentence.Id)
                .OrderBy(_ => Random.Shared.Next())
                .Take(3)
                .Select(s => new QuizSentence(s.Text, s.MatchStart, s.MatchLength, s.SourceLabel))
                .ToList();
            var translations = word.GetTranslations();
            var correct = Pick(translations);

            var distractors = PickDistractors(word, correct, words, Math.Max(1, optionsCount - 1));
            var options = distractors.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();

            questions.Add(new QuizQuestion(
                word.Id,
                sentence.Id,
                word.English,
                sentence.Text,
                sentence.MatchStart,
                sentence.MatchLength,
                options,
                correct,
                word.Translation,
                extra,
                sentence.SourceLabel));
        }

        return questions;
    }

    public async Task<QuizResult> FinishAsync(
        IReadOnlyList<QuizQuestion> questions,
        IReadOnlyList<string?> chosen,
        IReadOnlyList<QuizHint> hints,
        DateTime startedAt)
    {
        var finishedAt = DateTime.UtcNow;
        var wordIds = questions.Select(q => q.WordId).Distinct().ToList();
        var before = await _quizzes.GetWordProgressAsync(wordIds);
        var answers = new List<QuizAnswer>();
        var rows = new List<QuizResultRow>();

        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var choice = i < chosen.Count ? chosen[i] : null;
            var isCorrect = question.IsCorrect(choice);
            var hint = i < hints.Count ? hints[i] : QuizHint.None;

            answers.Add(new QuizAnswer
            {
                WordId = question.WordId,
                SentenceId = question.SentenceId,
                ChosenTranslation = choice ?? "",
                CorrectTranslation = question.AllTranslations,
                CorrectOption = question.CorrectOption,
                Hint = hint,
                IsCorrect = isCorrect,
                AnsweredAt = finishedAt,
            });

            rows.Add(new QuizResultRow(
                i + 1,
                question.English,
                choice ?? "—",
                question.CorrectOption,
                question.AllTranslations,
                isCorrect,
                hint));
        }

        var session = new QuizSession
        {
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            QuestionCount = questions.Count,
        };

        await _quizzes.SaveSessionAsync(session, answers);

        // Какие слова сменили стадию изучения из-за этого теста.
        var after = await _quizzes.GetWordProgressAsync(wordIds);
        var changes = new List<WordStageChange>();
        foreach (var question in questions.DistinctBy(q => q.WordId))
        {
            var from = before.GetValueOrDefault(question.WordId, WordProgress.Empty).Stage;
            var to = after.GetValueOrDefault(question.WordId, WordProgress.Empty).Stage;
            if (from != to)
            {
                changes.Add(new WordStageChange(question.English, from, to));
            }
        }

        return new QuizResult(rows, changes);
    }

    private static readonly QuizStrategy[] RandomPool =
        [QuizStrategy.Normal, QuizStrategy.Hardest, QuizStrategy.LeastPlayed, QuizStrategy.Forgotten, QuizStrategy.Newest];

    /// <summary>Выбирает слова по стратегии; если подходящих слов не хватает, остаток добирается обычным взвешенным выбором.</summary>
    private static List<Word> PickWords(List<Word> candidates, int count, QuizStrategy strategy)
    {
        if (strategy == QuizStrategy.Random)
        {
            strategy = RandomPool[Random.Shared.Next(RandomPool.Length)];
        }

        List<Word> primary = strategy switch
        {
            QuizStrategy.Hardest => TakeFromTop(
                candidates.Where(w => w.WrongCount > 0)
                    .OrderByDescending(w => (double)w.WrongCount / (w.CorrectCount + w.WrongCount))
                    .ThenByDescending(w => w.WrongCount),
                count),
            QuizStrategy.LeastPlayed => TakeFromTop(
                candidates.OrderBy(w => w.CorrectCount + w.WrongCount).ThenBy(_ => Random.Shared.Next()),
                count,
                poolFactor: 1),
            QuizStrategy.Forgotten => TakeFromTop(
                candidates.Where(w => w.LastAskedAt is not null).OrderBy(w => w.LastAskedAt),
                count),
            QuizStrategy.Newest => TakeFromTop(candidates.OrderByDescending(w => w.AddedAt), count, poolFactor: 1),
            _ => [],
        };

        if (strategy == QuizStrategy.Normal || primary.Count == 0)
        {
            return PickWeighted(candidates, count);
        }

        if (primary.Count < count)
        {
            var taken = primary.Select(w => w.Id).ToHashSet();
            primary.AddRange(PickWeighted(candidates.Where(w => !taken.Contains(w.Id)).ToList(), count - primary.Count));
        }

        return primary.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    /// <summary>
    /// Берёт слова из начала упорядоченного списка. Пул чуть больше нужного (в 2 раза),
    /// чтобы два квиза подряд не состояли из одних и тех же слов.
    /// </summary>
    private static List<Word> TakeFromTop(IEnumerable<Word> ordered, int count, int poolFactor = 2) =>
        ordered.Take(count * poolFactor).OrderBy(_ => Random.Shared.Next()).Take(count).ToList();

    /// <summary>
    /// Вес слова: базовая единица, плюс доля ошибок, плюс бонус за непроверенные слова,
    /// плюс бонус за давность последней проверки (до 30 дней).
    /// </summary>
    private static double Weight(Word word)
    {
        var asked = word.CorrectCount + word.WrongCount;
        var weight = 1.0;

        if (asked == 0)
        {
            weight += 2.0;
        }
        else
        {
            weight += 3.0 * word.WrongCount / asked;
        }

        if (word.LastAskedAt is { } last)
        {
            var days = Math.Clamp((DateTime.UtcNow - last.ToUniversalTime()).TotalDays, 0, 30);
            weight += days / 30.0;
        }

        return weight;
    }

    /// <summary>Выбор без повторов с вероятностью пропорциональной весу.</summary>
    private static List<Word> PickWeighted(List<Word> candidates, int count)
    {
        var pool = candidates.ToList();
        var result = new List<Word>(count);

        while (result.Count < count && pool.Count > 0)
        {
            var total = pool.Sum(Weight);
            var roll = Random.Shared.NextDouble() * total;
            var index = 0;

            for (; index < pool.Count - 1; index++)
            {
                roll -= Weight(pool[index]);
                if (roll <= 0)
                {
                    break;
                }
            }

            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// Неверные варианты берутся из переводов других слов и подбираются похожими на верный
    /// (длина, начальная буква, окончание), с небольшим случайным разбросом.
    /// Переводы самого слова в дистракторы не попадают.
    /// </summary>
    private static List<string> PickDistractors(Word word, string correct, IReadOnlyList<Word> allWords, int count)
    {
        var forbidden = new HashSet<string>(word.GetTranslations(), StringComparer.OrdinalIgnoreCase);

        var pool = allWords
            .Where(w => w.Id != word.Id)
            .SelectMany(w => w.GetTranslations())
            .Where(t => !forbidden.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return pool
            .Select(t => (Text: t, Score: Similarity(correct, t) + Random.Shared.NextDouble() * 3.0))
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.Text)
            .ToList();
    }

    private static double Similarity(string a, string b)
    {
        var score = -Math.Abs(a.Length - b.Length) * 0.5;

        if (a.Length > 0 && b.Length > 0 && char.ToLowerInvariant(a[0]) == char.ToLowerInvariant(b[0]))
        {
            score += 1.0;
        }

        if (a.Length >= 2 && b.Length >= 2 &&
            string.Equals(a[^2..], b[^2..], StringComparison.OrdinalIgnoreCase))
        {
            score += 3.0;
        }

        // одинаковое число слов («по-прежнему» и «в то же время» не должны смешиваться с одиночными словами)
        if (a.Count(c => c == ' ') == b.Count(c => c == ' '))
        {
            score += 1.0;
        }

        return score;
    }

    private static T Pick<T>(IReadOnlyList<T> items) => items[Random.Shared.Next(items.Count)];
}
