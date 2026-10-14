using EngMemAssist.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Integrations;

public static class IntegrationsServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<IWordSource, GoogleSheetsWordSource>();

        services.AddSingleton<ISentenceProvider>(_ =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("EngMemAssist/1.0");
            return new TatoebaSentenceProvider(http);
        });

        return services;
    }
}
