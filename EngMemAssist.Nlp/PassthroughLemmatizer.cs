using EngMemAssist.Core.Abstractions;

namespace EngMemAssist.Nlp;

/// <summary>
/// ВРЕМЕННАЯ реализация: только обрезает пробелы и приводит к нижнему регистру.
/// Будет заменена лемматизатором на Catalyst на шаге синхронизации слов.
/// </summary>
public sealed class PassthroughLemmatizer : ILemmatizer
{
    public string Lemmatize(string word) => word.Trim().ToLowerInvariant();
}
