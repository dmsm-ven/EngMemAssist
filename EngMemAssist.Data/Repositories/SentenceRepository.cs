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

    public async Task<int> AddRangeAsync(IReadOnlyList<Sentence> sentences)
    {
        if (sentences.Count == 0)
        {
            return 0;
        }

        await using var connection = _factory.Create();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // Источники: API создаются один раз, локальная база — при первом появлении своего имени.
        var sourceIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in sentences.Select(s => s.Source).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var isLocal = tag.StartsWith("local:", StringComparison.OrdinalIgnoreCase);
            var name = isLocal ? tag[6..] : CanonicalApiName(tag);
            var kind = isLocal ? 1 : 0;

            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO SentenceSources (Kind, Name) VALUES (@kind, @name)",
                new { kind, name },
                transaction);
            sourceIds[tag] = await connection.ExecuteScalarAsync<long>(
                "SELECT Id FROM SentenceSources WHERE Kind = @kind AND Name = @name",
                new { kind, name },
                transaction);
        }

        foreach (var sentence in sentences)
        {
            sentence.SourceId = sourceIds[sentence.Source];
        }

        var inserted = await connection.ExecuteAsync(
            """
            INSERT OR IGNORE INTO Sentences (WordId, Text, Translation, Source, SourceId, MatchStart, MatchLength)
            VALUES (@WordId, @Text, @Translation, @Source, @SourceId, @MatchStart, @MatchLength)
            """,
            sentences,
            transaction);

        await transaction.CommitAsync();
        return inserted;
    }

    private static string CanonicalApiName(string tag) => tag.ToLowerInvariant() switch
    {
        "tatoeba" => "Tatoeba",
        "wiktionary" => "Wiktionary",
        "" => "Неизвестный API",
        var other => char.ToUpperInvariant(other[0]) + other[1..],
    };

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
            SELECT s.Id, s.WordId, s.Text, s.Translation, s.Source, s.MatchStart, s.MatchLength,
                   s.SourceId, ss.Name AS SourceName, COALESCE(ss.Kind, 0) AS SourceKind
            FROM Sentences s
            LEFT JOIN SentenceSources ss ON ss.Id = s.SourceId
            WHERE s.WordId IN @ids AND s.MatchStart >= 0
            """,
            new { ids = wordIds });
        return rows.AsList();
    }
}
