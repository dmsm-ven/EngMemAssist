using DbUp;
using System.Reflection;

namespace EngMemAssist.Data;

public interface IDatabaseMigrator
{
    void Migrate();
}

/// <summary>Применяет SQL-скрипты из Migrations/*.sql (встроенные ресурсы) по порядку имён.</summary>
public sealed class DatabaseMigrator : IDatabaseMigrator
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public DatabaseMigrator(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Migrate()
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(_connectionFactory.ConnectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .LogToNowhere()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Не удалось применить миграции базы данных.", result.Error);
        }
    }
}
