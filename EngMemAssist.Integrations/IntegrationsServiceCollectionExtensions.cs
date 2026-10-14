using EngMemAssist.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Integrations;

public static class IntegrationsServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<IWordSource, GoogleSheetsWordSource>();

        services.AddSingleton<ISentenceProvider>(_ => new CompositeSentenceProvider(
        [
            new TatoebaSentenceProvider(new ThrottledHttpClient(CreateHttpClient(), TimeSpan.FromMilliseconds(400))),
            new WiktionarySentenceProvider(new ThrottledHttpClient(CreateHttpClient(), TimeSpan.FromMilliseconds(250))),
        ]));

        return services;
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        // Wikimedia требует осмысленный User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("EngMemAssist/1.0 (personal English learning app)");
        return http;
    }
}
