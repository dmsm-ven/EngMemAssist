using Dapper;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.Data.Repositories;

public sealed class QuizRepository : IQuizRepository
{
    private readonly ISqliteConnectionFactory _factory;

    public QuizRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task SaveSessionAsync(QuizSession session, IReadOnlyList<QuizAnswer> answers)
    {
        await using var connection = _factory.Create();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        session.Id = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO QuizSessions (StartedAt, FinishedAt, QuestionCount)
            VALUES (@StartedAt, @FinishedAt, @QuestionCount);
            SELECT last_insert_rowid();
            """,
            session,
            transaction);

        foreach (var answer in answers)
        {
            answer.SessionId = session.Id;
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO QuizAnswers (SessionId, WordId, SentenceId, ChosenTranslation, CorrectTranslation, CorrectOption, Hint, IsCorrect, AnsweredAt)
            VALUES (@SessionId, @WordId, @SentenceId, @ChosenTranslation, @CorrectTranslation, @CorrectOption, @Hint, @IsCorrect, @AnsweredAt)
            """,
            answers,
            transaction);

        var finishedAt = session.FinishedAt ?? DateTime.UtcNow;
        foreach (var answer in answers)
        {
            await connection.ExecuteAsync(
                """
                UPDATE Words
                SET CorrectCount = CorrectCount + @correct,
                    WrongCount = WrongCount + @wrong,
                    LastAskedAt = @asked
                WHERE Id = @wordId
                """,
                new
                {
                    correct = answer.IsCorrect ? 1 : 0,
                    wrong = answer.IsCorrect ? 0 : 1,
                    asked = finishedAt,
                    wordId = answer.WordId,
                },
                transaction);
        }

        await transaction.CommitAsync();
    }

    public async Task<StatsOverview> GetOverviewAsync()
    {
        await using var connection = _factory.Create();
        return await connection.QuerySingleAsync<StatsOverview>(
            """
            SELECT
                (SELECT COUNT(*) FROM QuizSessions)                      AS SessionsCount,
                (SELECT COUNT(*) FROM QuizAnswers)                       AS AnswersCount,
                (SELECT COUNT(*) FROM QuizAnswers WHERE IsCorrect = 1)   AS CorrectAnswers,
                (SELECT COUNT(*) FROM Words WHERE IsRemoved = 0)         AS ActiveWords
            """);
    }

    public async Task<IReadOnlyList<SessionSummary>> GetRecentSessionsAsync(int take)
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<SessionSummary>(
            """
            SELECT s.Id, s.StartedAt, s.QuestionCount,
                   COALESCE(SUM(a.IsCorrect), 0) AS CorrectCount,
                   COALESCE(SUM(CASE WHEN a.Hint > 0 THEN 1 ELSE 0 END), 0) AS HintsUsed
            FROM QuizSessions s
            LEFT JOIN QuizAnswers a ON a.SessionId = s.Id
            GROUP BY s.Id
            ORDER BY s.StartedAt DESC
            LIMIT @take
            """,
            new { take });
        return rows.AsList();
    }

    public async Task<QuizResult> GetSessionResultAsync(long sessionId)
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<AnswerRow>(
            """
            SELECT w.English                                          AS English,
                   a.ChosenTranslation                                AS Chosen,
                   COALESCE(a.CorrectOption, a.CorrectTranslation)    AS CorrectOption,
                   a.CorrectTranslation                               AS AllTranslations,
                   a.IsCorrect                                        AS IsCorrect,
                   a.Hint                                             AS Hint
            FROM QuizAnswers a
            JOIN Words w ON w.Id = a.WordId
            WHERE a.SessionId = @sessionId
            ORDER BY a.Id
            """,
            new { sessionId });

        var result = rows
            .Select((r, i) => new QuizResultRow(
                i + 1,
                r.English,
                string.IsNullOrEmpty(r.Chosen) ? "—" : r.Chosen,
                r.CorrectOption,
                r.AllTranslations,
                r.IsCorrect,
                Enum.IsDefined(typeof(QuizHint), r.Hint) ? (QuizHint)r.Hint : QuizHint.None))
            .ToList();

        return new QuizResult(result);
    }

    private sealed class AnswerRow
    {
        public string English { get; set; } = "";
        public string Chosen { get; set; } = "";
        public string CorrectOption { get; set; } = "";
        public string AllTranslations { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int Hint { get; set; }
    }

    public async Task<IReadOnlyList<WordDifficulty>> GetWordStatsAsync()
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<WordDifficulty>(
            """
            SELECT English, Translation, CorrectCount, WrongCount
            FROM Words
            WHERE IsRemoved = 0
            ORDER BY English COLLATE NOCASE
            """);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<WordDifficulty>> GetHardestWordsAsync(int take)
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<WordDifficulty>(
            """
            SELECT English, Translation, CorrectCount, WrongCount
            FROM Words
            WHERE IsRemoved = 0 AND WrongCount > 0
            ORDER BY (1.0 * WrongCount / (CorrectCount + WrongCount)) DESC, WrongCount DESC
            LIMIT @take
            """,
            new { take });
        return rows.AsList();
    }
}
