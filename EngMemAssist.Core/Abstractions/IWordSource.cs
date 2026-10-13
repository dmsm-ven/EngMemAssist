namespace EngMemAssist.Core.Abstractions;

/// <summary>Пара из таблицы-источника до нормализации.</summary>
public sealed record SourceWord(string English, string Translation);

/// <summary>Источник слов (сейчас Google Spreadsheet, позже можно заменить).</summary>
public interface IWordSource
{
    Task<IReadOnlyList<SourceWord>> LoadAsync(CancellationToken cancellationToken = default);
}
