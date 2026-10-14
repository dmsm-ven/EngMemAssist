namespace EngMemAssist.Core.Abstractions;

/// <summary>Слово из текста вместе с нормальной формой и позицией в исходной строке.</summary>
public sealed record LemmaToken(string Text, string Lemma, int Start, int Length);

/// <summary>Приводит английский текст к нормальным формам слов (children -> child).</summary>
public interface ILemmatizer
{
    /// <summary>Разбивает текст на слова и возвращает для каждого лемму и позицию. Знаки препинания пропускаются.</summary>
    IReadOnlyList<LemmaToken> Analyze(string text);

    /// <summary>Нормальная форма слова или фразы (леммы слов через пробел, в нижнем регистре).</summary>
    string Lemmatize(string text);
}

/// <summary>Необязательный интерфейс: лемматизатор сообщает, что работает в упрощённом режиме.</summary>
public interface ILemmatizerDiagnostics
{
    string? DegradedReason { get; }
}
