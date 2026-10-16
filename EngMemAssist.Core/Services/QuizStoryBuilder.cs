using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Services;

/// <summary>Итог теста в виде короткого рассказа: заголовок и абзацы.</summary>
public sealed record QuizStory(string Emoji, string Headline, IReadOnlyList<string> Paragraphs);

public static class QuizStoryBuilder
{
    public static QuizStory Build(QuizResult result)
    {
        var total = result.Total;
        var correct = result.CorrectCount;
        var percent = total == 0 ? 0 : 100.0 * correct / total;
        var paragraphs = new List<string>();

        var (emoji, headline) = percent switch
        {
            >= 100 => ("🏆", $"Безупречно! {correct} из {total}"),
            >= 80 => ("🎉", $"Отличный результат: {correct} из {total}"),
            >= 50 => ("💪", $"Хорошо: {correct} из {total}, но есть над чем поработать"),
            _ => ("🌧️", $"Непростой день: {correct} из {total}. Зато ошибки — лучший способ запомнить"),
        };

        // Прогресс по стадиям слов.
        var promoted = result.StageChanges.Where(c => c.IsPromotion).ToList();
        var learned = promoted.Where(c => c.To == WordStage.Learned).ToList();
        var others = promoted.Except(learned).ToList();
        var demoted = result.StageChanges.Where(c => !c.IsPromotion).ToList();

        if (learned.Count > 0)
        {
            paragraphs.Add($"⭐ Выучено: {Names(learned.Select(c => c.English))}. Эти слова теперь можно считать своими.");
        }

        if (others.Count > 0)
        {
            var parts = others.Take(3).Select(c => $"«{c.English}» — {c.To.Title().ToLowerInvariant()}");
            var more = others.Count > 3 ? $" и ещё {others.Count - 3}" : "";
            paragraphs.Add($"📈 Шаг вперёд: {string.Join(", ", parts)}{more}.");
        }

        if (demoted.Count > 0)
        {
            paragraphs.Add($"↩️ Вернулись на шаг назад: {Names(demoted.Select(c => c.English))}. Ничего страшного — они попадутся в ближайших квизах.");
        }

        // Лучшая серия верных ответов подряд.
        var best = 0;
        var run = 0;
        foreach (var row in result.Rows)
        {
            run = row.IsCorrect ? run + 1 : 0;
            best = Math.Max(best, run);
        }

        if (best >= 3 && best < total)
        {
            paragraphs.Add($"🔥 Лучшая серия: {best} верных ответов подряд.");
        }

        // Ошибки.
        var wrong = result.Rows.Where(r => !r.IsCorrect).ToList();
        if (wrong.Count > 0)
        {
            var parts = wrong.Take(4).Select(r => $"«{r.English}» — {r.CorrectOption}");
            var more = wrong.Count > 4 ? $" и ещё {wrong.Count - 4}" : "";
            paragraphs.Add($"🔍 Стоит повторить: {string.Join("; ", parts)}{more}.");
        }

        // Подсказки.
        var hints = result.Rows.Where(r => r.Hint != QuizHint.None).ToList();
        if (hints.Count > 0)
        {
            var icons = string.Join(" ", hints.Select(r => r.HintIcon));
            var noHint = result.Rows.Count(r => r.Hint == QuizHint.None && r.IsCorrect);
            paragraphs.Add($"💡 Подсказок использовано: {hints.Count} ({icons}). Без подсказок верно: {noHint}.");
        }
        else if (total >= 5 && percent >= 80)
        {
            paragraphs.Add("💡 И всё это без единой подсказки.");
        }

        if (paragraphs.Count == 0)
        {
            paragraphs.Add("Продолжай в том же духе — регулярность важнее длины занятий.");
        }

        return new QuizStory(emoji, headline, paragraphs);
    }

    private static string Names(IEnumerable<string> words)
    {
        var list = words.ToList();
        var shown = string.Join(", ", list.Take(4).Select(w => $"«{w}»"));
        return list.Count > 4 ? $"{shown} и ещё {list.Count - 4}" : shown;
    }
}
