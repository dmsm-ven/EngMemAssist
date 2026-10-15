namespace EngMemAssist.Core.Models;

/// <summary>Подсказка, взятая на вопрос. Значение хранится в БД (QuizAnswers.Hint), числа менять нельзя.</summary>
public enum QuizHint
{
    None = 0,

    /// <summary>Убрана половина неверных вариантов.</summary>
    FiftyFifty = 1,

    /// <summary>Показано ещё одно предложение со словом.</summary>
    ExtraContext = 2,

    /// <summary>Показана первая буква верного варианта.</summary>
    FirstLetter = 3,
}

public static class QuizHintExtensions
{
    /// <summary>Короткий значок для таблиц и кнопок.</summary>
    public static string Icon(this QuizHint hint) => hint switch
    {
        QuizHint.FiftyFifty => "½",
        QuizHint.ExtraContext => "💬",
        QuizHint.FirstLetter => "🔤",
        _ => "",
    };

    public static string Description(this QuizHint hint) => hint switch
    {
        QuizHint.FiftyFifty => "Убрать половину неверных вариантов",
        QuizHint.ExtraContext => "Подсказка: показано дополнительное предложение",
        QuizHint.FirstLetter => "Подсказка: показана первая буква ответа",
        _ => "",
    };

    /// <summary>Сколько подсказок разрешено на тест: число вопросов / 5 (целочисленное деление).</summary>
    public static int Budget(int questionCount) => Math.Max(0, questionCount / 5);
}
