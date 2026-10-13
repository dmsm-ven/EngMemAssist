using Microsoft.Data.Sqlite;

namespace EngMemAssist.Data;

/// <summary>Создаёт подключения к SQLite. Репозитории на Dapper получают соединение отсюда.</summary>
public interface ISqliteConnectionFactory
{
    string ConnectionString { get; }

    SqliteConnection Create();
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    public SqliteConnectionFactory(string databaseFile)
    {
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile,
            ForeignKeys = true,
        }.ToString();
    }

    public string ConnectionString { get; }

    public SqliteConnection Create() => new(ConnectionString);
}
