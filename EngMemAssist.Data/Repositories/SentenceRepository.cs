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

    public async Task<IReadOnlyDictionary<long, int>> CountByWordAsync()
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<CountRow>(
            "SELECT WordId, COUNT(*) AS Total FROM Sentences WHERE MatchStart >= 0 GROUP BY WordId");
        return rows.ToDictionary(r => r.WordId, r => r.Total);
    }

    private sealed class CountRow
    {
        public long WordId { get; set; }
        public int Total { get; set; }
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
