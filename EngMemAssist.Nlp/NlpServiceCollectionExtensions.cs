using EngMemAssist.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Nlp;

public static class NlpServiceCollectionExtensions
{
    public static IServiceCollection AddNlp(this IServiceCollection services)
    {
        services.AddSingleton<CatalystLemmatizer>();
        services.AddSingleton<ILemmatizer>(sp => sp.GetRequiredService<CatalystLemmatizer>());
        services.AddSingleton<ILemmatizerDiagnostics>(sp => sp.GetRequiredService<CatalystLemmatizer>());
        return services;
    }
}
