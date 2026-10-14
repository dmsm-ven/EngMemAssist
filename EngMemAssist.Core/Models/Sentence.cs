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

    public bool HasValidMatch =>
        MatchStart >= 0 && MatchLength > 0 && MatchStart + MatchLength <= Text.Length;
}
