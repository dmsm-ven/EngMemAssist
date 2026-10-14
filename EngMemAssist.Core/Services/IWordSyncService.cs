namespace EngMemAssist.Core.Services;

public sealed record SyncResult(
    int Added,
    int Updated,
    int Removed,
    int SentencesAdded,
    int FailedWords,
    int TotalActive,
    bool RateLimited,
    string? RateLimitMessage);

public interface IWordSyncService
{
    /// <summary>
    /// Сверяет локальный список слов с таблицей, добавляет новые слова и подгружает для них предложения.
    /// </summary>
    /// <exception cref="Exceptions.SpreadsheetAccessException">Таблица недоступна.</exception>
    Task<SyncResult> SyncAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
