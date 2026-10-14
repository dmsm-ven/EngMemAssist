using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Abstractions;

public interface IWordRepository
{
    /// <summary>Все слова, включая помеченные как удалённые.</summary>
    Task<IReadOnlyList<Word>> GetAllAsync();

    /// <summary>Слова, которые есть в таблице сейчас.</summary>
    Task<IReadOnlyList<Word>> GetActiveAsync();

    Task InsertAsync(Word word);

    /// <summary>Обновляет лемму, английское слово, перевод, флаг удаления и дату загрузки предложений. Счётчики квиза не трогает.</summary>
    Task UpdateAsync(Word word);
}

public interface ISentenceRepository
{
    Task AddRangeAsync(IReadOnlyList<Sentence> sentences);

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

    /// <summary>Таблица ответов прошедшего теста — как на экране результатов сразу после теста.</summary>
    Task<QuizResult> GetSessionResultAsync(long sessionId);
}
