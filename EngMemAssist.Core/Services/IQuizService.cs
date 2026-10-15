using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Services;

public interface IQuizService
{
    /// <summary>Собирает квиз из слов, у которых есть предложения. Слова с ошибками и давно не проверявшиеся выпадают чаще.</summary>
    /// <exception cref="InvalidOperationException">Нет слов, пригодных для квиза.</exception>
    Task<IReadOnlyList<QuizQuestion>> CreateQuizAsync(int questionCount, int optionsCount, QuizStrategy strategy = QuizStrategy.Normal);

    /// <summary>Сохраняет сессию и ответы в БД и возвращает таблицу результатов. chosen[i] — ответ на вопрос i (null — нет ответа), hints[i] — взятая подсказка.</summary>
    Task<QuizResult> FinishAsync(IReadOnlyList<QuizQuestion> questions, IReadOnlyList<string?> chosen, IReadOnlyList<QuizHint> hints, DateTime startedAt);
}
