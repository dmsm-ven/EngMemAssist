using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Регистрирует доступ к SQLite (Dapper-репозитории будут добавляться сюда же).</summary>
    public static IServiceCollection AddData(this IServiceCollection services, string databaseFile)
    {
        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(databaseFile));
        services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();
        return services;
    }
}
