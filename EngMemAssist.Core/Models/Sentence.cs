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
}
