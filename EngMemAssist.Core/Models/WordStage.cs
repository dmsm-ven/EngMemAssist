namespace EngMemAssist.Core.Models;

/// <summary>Стадия изучения слова. Вычисляется по истории ответов, в базе не хранится.</summary>
public enum WordStage
{
    /// <summary>Слово ещё ни разу не попадалось в квизах.</summary>
    New = 0,

    /// <summary>Слово уже было в квизах, но уверенного знания пока нет.</summary>
    Learning = 1,

    /// <summary>Несколько верных ответов подряд.</summary>
    Familiar = 2,

    /// <summary>Много верных ответов подряд: слово выучено.</summary>
    Learned = 3,
}

/// <summary>Сводка по ответам на слово: сколько всего и сколько верных подряд в конце истории.</summary>
public sealed record WordProgress(int Answers, int Streak)
{
    public static readonly WordProgress Empty = new(0, 0);

    public WordStage Stage => WordStages.From(Answers, Streak);
}

public static class WordStages
{
    /// <summary>Сколько верных ответов подряд нужно, чтобы слово считалось знакомым.</summary>
    public const int FamiliarStreak = 2;

    /// <summary>Сколько верных ответов подряд нужно, чтобы слово считалось выученным.</summary>
    public const int LearnedStreak = 5;

    public static WordStage From(int answers, int streak) =>
        answers == 0 ? WordStage.New
        : streak >= LearnedStreak ? WordStage.Learned
        : streak >= FamiliarStreak ? WordStage.Familiar
        : WordStage.Learning;

    public static string Title(this WordStage stage) => stage switch
    {
        WordStage.New => "Новое",
        WordStage.Learning => "Изучается",
        WordStage.Familiar => "Знакомо",
        WordStage.Learned => "Выучено",
        _ => "",
    };

    public static string Icon(this WordStage stage) => stage switch
    {
        WordStage.New => "🌱",
        WordStage.Learning => "📖",
        WordStage.Familiar => "👍",
        WordStage.Learned => "⭐",
        _ => "",
    };

    public static string Description(this WordStage stage) => stage switch
    {
        WordStage.New => "Ещё не было в квизах",
        WordStage.Learning => "Уже проверялось, но уверенного знания пока нет",
        WordStage.Familiar => $"{FamiliarStreak}+ верных ответов подряд",
        WordStage.Learned => $"{LearnedStreak}+ верных ответов подряд",
        _ => "",
    };

    public static readonly WordStage[] All = [WordStage.New, WordStage.Learning, WordStage.Familiar, WordStage.Learned];
}
