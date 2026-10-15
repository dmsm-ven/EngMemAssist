namespace EngMemAssist.Core.Models;

/// <summary>Проверочное предложение для слова (из внешнего API).</summary>
public sealed class Sentence
{
    public long Id { get; set; }
    public long WordId { get; set; }
    public string Text { get; set; } = "";
    public string? Translation { get; set; }

    /// <summary>Откуда взято предложение, например "tatoeba".</summary>
    public string Source { get; set; } = "";

    /// <summary>Позиция слова в тексте (для выделения в квизе). -1 — не найдено.</summary>
    public int MatchStart { get; set; } = -1;

    public int MatchLength { get; set; }

    /// <summary>Ссылка на SentenceSources.Id (заполняется при сохранении по значению <see cref="Source"/>).</summary>
    public long? SourceId { get; set; }

    /// <summary>Название источника из справочника (только при чтении из БД).</summary>
    public string? SourceName { get; set; }

    /// <summary>0 — внешний API, 1 — локальная текстовая база (только при чтении из БД).</summary>
    public int SourceKind { get; set; }

    /// <summary>Подпись источника для подсказки в квизе.</summary>
    public string SourceLabel => string.IsNullOrEmpty(SourceName)
        ? "Источник неизвестен"
        : SourceKind == 1 ? $"Локальная текстовая база «{SourceName}»" : $"API: {SourceName}";

    public bool HasValidMatch =>
        MatchStart >= 0 && MatchLength > 0 && MatchStart + MatchLength <= Text.Length;
}
