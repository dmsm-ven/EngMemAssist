using EngMemAssist.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Nlp;

public static class NlpServiceCollectionExtensions
{
    public static IServiceCollection AddNlp(this IServiceCollection services)
    {
        services.AddSingleton<ILemmatizer, PassthroughLemmatizer>();
        return services;
    }
}
