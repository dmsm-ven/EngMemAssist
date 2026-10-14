using EngMemAssist.Core.Abstractions;
using EngMemAssist.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Регистрирует доступ к SQLite и Dapper-репозитории.</summary>
    public static IServiceCollection AddData(this IServiceCollection services, string databaseFile)
    {
        DapperSetup.Initialize();

        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(databaseFile));
        services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();

        services.AddSingleton<IWordRepository, WordRepository>();
        services.AddSingleton<ISentenceRepository, SentenceRepository>();
        services.AddSingleton<IQuizRepository, QuizRepository>();
        return services;
    }
}
