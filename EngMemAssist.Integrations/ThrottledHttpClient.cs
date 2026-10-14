using System.Net;

namespace EngMemAssist.Integrations;

/// <summary>
/// HttpClient с минимальным интервалом между запросами и повтором при 429 (Too Many Requests).
/// Интервал защищает от лимита при первой синхронизации большого списка слов.
/// </summary>
public sealed class ThrottledHttpClient
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http;
    private readonly TimeSpan _minInterval;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastRequestAt = DateTime.MinValue;

    public ThrottledHttpClient(HttpClient http, TimeSpan minInterval)
    {
        _http = http;
        _minInterval = minInterval;
    }

    /// <summary>Выполняет GET. Ответ 429 возвращается вызывающему только после исчерпания повторов.</summary>
    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                var wait = _minInterval - (DateTime.UtcNow - _lastRequestAt);
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }

                response = await _http.GetAsync(url, cancellationToken);
                _lastRequestAt = DateTime.UtcNow;
            }
            finally
            {
                _gate.Release();
            }

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MaxRetries)
            {
                return response;
            }

            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(3 * (attempt + 1));
            if (delay > MaxRetryDelay)
            {
                delay = MaxRetryDelay;
            }

            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }
    }
}
