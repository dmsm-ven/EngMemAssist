using EngMemAssist.Core.Abstractions;

namespace EngMemAssist.Core.Services;

/// <summary>Ищет в предложении слово (в любой форме) и возвращает его позицию для пропуска.</summary>
public static class SentenceMatcher
{
    /// <summary>
    /// Слово считается найденным, если лемма слова из предложения совпала с леммой искомого слова
    /// или совпала сама форма (страховка на случай, если лемматизатор в контексте дал другой результат).
    /// Для фраз слова должны идти подряд.
    /// </summary>
    public static bool TryFind(IReadOnlyList<LemmaToken> tokens, WordPattern pattern, out int start, out int length)
    {
        start = -1;
        length = 0;

        var count = pattern.Lemmas.Count;
        if (count == 0 || tokens.Count < count)
        {
            return false;
        }

        for (var i = 0; i <= tokens.Count - count; i++)
        {
            var matched = true;
            for (var j = 0; j < count; j++)
            {
                var token = tokens[i + j];
                var lemmaMatches = string.Equals(token.Lemma, pattern.Lemmas[j], StringComparison.OrdinalIgnoreCase);
                var formMatches = j < pattern.Forms.Count &&
                                  string.Equals(token.Text, pattern.Forms[j], StringComparison.OrdinalIgnoreCase);
                if (!lemmaMatches && !formMatches)
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
            {
                continue;
            }

            var last = tokens[i + count - 1];
            start = tokens[i].Start;
            length = last.Start + last.Length - start;
            return true;
        }

        return false;
    }
}
