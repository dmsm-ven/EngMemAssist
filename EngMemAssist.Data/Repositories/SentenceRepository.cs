using Dapper;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.Data.Repositories;

public sealed class SentenceRepository : ISentenceRepository
{
    private readonly ISqliteConnectionFactory _factory;

    public SentenceRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task AddRangeAsync(IReadOnlyList<Sentence> sentences)
    {
        if (sentences.Count == 0)
        {
            return;
        }

        await using var connection = _factory.Create();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(
            """
            INSERT OR IGNORE INTO Sentences (WordId, Text, Translation, Source, MatchStart, MatchLength)
            VALUES (@WordId, @Text, @Translation, @Source, @MatchStart, @MatchLength)
            """,
            sentences,
            transaction);

        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<Sentence>> GetByWordIdsAsync(IReadOnlyCollection<long> wordIds)
    {
        if (wordIds.Count == 0)
        {
            return [];
        }

        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<Sentence>(
            """
            SELECT Id, WordId, Text, Translation, Source, MatchStart, MatchLength
            FROM Sentences
            WHERE WordId IN @ids AND MatchStart >= 0
            """,
            new { ids = wordIds });
        return rows.AsList();
    }
}
