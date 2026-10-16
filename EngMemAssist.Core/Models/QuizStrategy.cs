namespace EngMemAssist.Core.Models;

/// <summary>Как выбираются слова для квиза. Числа хранятся в settings.json, менять нельзя.</summary>
public enum QuizStrategy
{
    /// <summary>Взвешенный случайный выбор: чаще ошибочные, непроверенные и давно не повторявшиеся.</summary>
    Normal = 0,

    /// <summary>Слова с наибольшей долей ошибок.</summary>
    Hardest = 1,

    /// <summary>Ни разу не участвовавшие в квизах или участвовавшие реже всего.</summary>
    LeastPlayed = 2,

    /// <summary>Одна из остальных стратегий, выбранная случайно.</summary>
    Random = 3,

    /// <summary>Слова, которые проверялись давно: освежить в памяти.</summary>
    Forgotten = 4,

    /// <summary>Только самые последние добавленные в словарь слова.</summary>
    Newest = 5,
}

public static class QuizStrategyExtensions
{
    public static string Title(this QuizStrategy strategy) => strategy switch
    {
        QuizStrategy.Normal => "Обычная",
        QuizStrategy.Hardest => "Самые сложные слова",
        QuizStrategy.LeastPlayed => "Ещё не учавствовавшие",
        QuizStrategy.Forgotten => "Давно не повторял",
        QuizStrategy.Newest => "Самые новые слова",
        QuizStrategy.Random => "Случайная стратегия",
        _ => strategy.ToString(),
    };

    public static string Description(this QuizStrategy strategy) => strategy switch
    {
        QuizStrategy.Normal => "Случайные слова: чаще те, где были ошибки, новые и давно не проверявшиеся",
        QuizStrategy.Hardest => "Слова с самой большой долей ошибок",
        QuizStrategy.LeastPlayed => "Слова, которых в квизах ещё не было, или они были реже всего",
        QuizStrategy.Forgotten => "Слова, которые проверялись раньше всех остальных",
        QuizStrategy.Newest => "Только последние добавленные в словарь слова",
        QuizStrategy.Random => "Любая из остальных стратегий, выбранная случайно",
        _ => "",
    };

    /// <summary>Порядок показа в списке выбора.</summary>
    public static readonly QuizStrategy[] All =
    [
        QuizStrategy.Normal,
        QuizStrategy.Hardest,
        QuizStrategy.LeastPlayed,
        QuizStrategy.Forgotten,
        QuizStrategy.Newest,
        QuizStrategy.Random, // всегда последней в списке
    ];
}
