using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Abstractions;

public interface IWordRepository
{
    /// <summary>Все слова, включая помеченные как удалённые.</summary>
    Task<IReadOnlyList<Word>> GetAllAsync();

    /// <summary>Слова, которые есть в таблице сейчас.</summary>
    Task<IReadOnlyList<Word>> GetActiveAsync();

    /// <summary>Сколько слов в словаре сейчас (без загрузки самих слов).</summary>
    Task<int> CountActiveAsync();

    Task InsertAsync(Word word);

    /// <summary>Обновляет лемму, английское слово, перевод, флаг удаления и дату загрузки предложений. Счётчики квиза не трогает.</summary>
    Task UpdateAsync(Word word);
}

public interface ISentenceRepository
{
    /// <summary>Добавляет предложения; дубли (то же слово и тот же текст без учёта регистра) пропускаются. Возвращает число реально добавленных.</summary>
    Task<int> AddRangeAsync(IReadOnlyList<Sentence> sentences);

    Task<IReadOnlyList<Sentence>> GetByWordIdsAsync(IReadOnlyCollection<long> wordIds);

    /// <summary>Сколько пригодных для квиза предложений у каждого слова (слов без предложений в словаре нет).</summary>
    Task<IReadOnlyDictionary<long, int>> CountByWordAsync();
}

public interface IQuizRepository
{
    /// <summary>В одной транзакции сохраняет сессию, ответы и обновляет счётчики слов.</summary>
    Task SaveSessionAsync(QuizSession session, IReadOnlyList<QuizAnswer> answers);

    Task<StatsOverview> GetOverviewAsync();

    Task<IReadOnlyList<SessionSummary>> GetRecentSessionsAsync(int take);

    Task<IReadOnlyList<WordDifficulty>> GetHardestWordsAsync(int take);

    /// <summary>
    /// Прогресс слов по истории ответов: число ответов и верных подряд в конце. Слов без ответов в словаре нет.
    /// wordIds = null — по всем словам.
    /// </summary>
    Task<IReadOnlyDictionary<long, WordProgress>> GetWordProgressAsync(IReadOnlyCollection<long>? wordIds = null);

    /// <summary>Сколько ответов дано в каждый из последних дней (по местному времени). Дни без ответов в словаре не представлены.</summary>
    Task<IReadOnlyDictionary<DateOnly, int>> GetDailyActivityAsync(int days);

    /// <summary>Статистика ответов по каждому слову словаря (в том числе по ещё не проверявшимся).</summary>
    Task<IReadOnlyList<WordDifficulty>> GetWordStatsAsync();

    /// <summary>Таблица ответов прошедшего теста — как на экране результатов сразу после теста.</summary>
    Task<QuizResult> GetSessionResultAsync(long sessionId);
}
