using Dapper;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.Data.Repositories;

public sealed class WordRepository : IWordRepository
{
    private const string SelectColumns =
        "Id, Lemma, English, Translation, AddedAt, IsRemoved, CorrectCount, WrongCount, LastAskedAt, SentencesFetchedAt";

    private readonly ISqliteConnectionFactory _factory;

    public WordRepository(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Word>> GetAllAsync()
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<Word>($"SELECT {SelectColumns} FROM Words ORDER BY English");
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Word>> GetActiveAsync()
    {
        await using var connection = _factory.Create();
        var rows = await connection.QueryAsync<Word>(
            $"SELECT {SelectColumns} FROM Words WHERE IsRemoved = 0 ORDER BY English");
        return rows.AsList();
    }

    public async Task InsertAsync(Word word)
    {
        await using var connection = _factory.Create();
        word.Id = await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO Words (Lemma, English, Translation, AddedAt, IsRemoved, CorrectCount, WrongCount, LastAskedAt, SentencesFetchedAt)
            VALUES (@Lemma, @English, @Translation, @AddedAt, @IsRemoved, @CorrectCount, @WrongCount, @LastAskedAt, @SentencesFetchedAt);
            SELECT last_insert_rowid();
            """,
            word);
    }

    public async Task UpdateAsync(Word word)
    {
        await using var connection = _factory.Create();
        await connection.ExecuteAsync(
            """
            UPDATE Words
            SET Lemma = @Lemma,
                English = @English,
                Translation = @Translation,
                IsRemoved = @IsRemoved,
                SentencesFetchedAt = @SentencesFetchedAt
            WHERE Id = @Id
            """,
            word);
    }
}
