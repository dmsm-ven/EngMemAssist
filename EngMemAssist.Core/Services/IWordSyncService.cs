namespace EngMemAssist.Core.Services;

public sealed record SyncResult(
    int Added,
    int Updated,
    int Removed,
    int SentencesAdded,
    int FailedWords,
    int TotalActive,
    bool RateLimited,
    string? RateLimitMessage,
    int WordsWithoutSentences);

public sealed record SupplementResult(int WordsChecked, int WordsUpdated, int SentencesAdded, int Sources);

public interface IWordSyncService
{
    /// <summary>
    /// Сверяет локальный список слов с таблицей, добавляет новые слова и подгружает для них предложения.
    /// </summary>
    /// <exception cref="Exceptions.SpreadsheetAccessException">Таблица недоступна.</exception>
    Task<SyncResult> SyncAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обходит все слова словаря и добавляет в базу новые предложения из локальных .txt-источников
    /// (даже если у слова уже достаточно предложений). Таблица и внешние API не используются; дубли не добавляются.
    /// </summary>
    Task<SupplementResult> SupplementFromLocalAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
