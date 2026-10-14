using System.Text.RegularExpressions;
using EngMemAssist.Core.Abstractions;

namespace EngMemAssist.Nlp;

/// <summary>
/// Запасной лемматизатор на правилах: словарь неправильных форм и простые суффиксные правила.
/// Используется, если модели Catalyst не загрузились. Менее точен, но не требует моделей.
/// </summary>
public sealed partial class RuleBasedLemmatizer : ILemmatizer
{
    private static readonly Dictionary<string, string> Irregular = new(StringComparer.OrdinalIgnoreCase)
    {
        ["am"] = "be", ["is"] = "be", ["are"] = "be", ["was"] = "be", ["were"] = "be", ["been"] = "be", ["being"] = "be",
        ["has"] = "have", ["had"] = "have", ["having"] = "have",
        ["does"] = "do", ["did"] = "do", ["done"] = "do", ["doing"] = "do",
        ["went"] = "go", ["gone"] = "go", ["goes"] = "go", ["going"] = "go",
        ["children"] = "child", ["men"] = "man", ["women"] = "woman", ["people"] = "person",
        ["mice"] = "mouse", ["feet"] = "foot", ["teeth"] = "tooth", ["geese"] = "goose",
        ["knives"] = "knife", ["lives"] = "life", ["wives"] = "wife", ["wolves"] = "wolf", ["leaves"] = "leaf",
        ["better"] = "good", ["best"] = "good", ["worse"] = "bad", ["worst"] = "bad",
        ["said"] = "say", ["made"] = "make", ["took"] = "take", ["taken"] = "take", ["came"] = "come",
        ["saw"] = "see", ["seen"] = "see", ["knew"] = "know", ["known"] = "know", ["got"] = "get", ["gotten"] = "get",
        ["gave"] = "give", ["given"] = "give", ["found"] = "find", ["thought"] = "think", ["told"] = "tell",
        ["became"] = "become", ["left"] = "leave", ["felt"] = "feel", ["brought"] = "bring", ["began"] = "begin",
        ["begun"] = "begin", ["kept"] = "keep", ["held"] = "hold", ["wrote"] = "write", ["written"] = "write",
        ["stood"] = "stand", ["heard"] = "hear", ["let"] = "let", ["meant"] = "mean", ["set"] = "set",
        ["met"] = "meet", ["ran"] = "run", ["paid"] = "pay", ["sat"] = "sit", ["spoke"] = "speak", ["spoken"] = "speak",
        ["lay"] = "lie", ["led"] = "lead", ["read"] = "read", ["grew"] = "grow", ["grown"] = "grow",
        ["lost"] = "lose", ["fell"] = "fall", ["fallen"] = "fall", ["sent"] = "send", ["built"] = "build",
        ["understood"] = "understand", ["drew"] = "draw", ["drawn"] = "draw", ["broke"] = "break", ["broken"] = "break",
        ["spent"] = "spend", ["rose"] = "rise", ["risen"] = "rise", ["drove"] = "drive", ["driven"] = "drive",
        ["bought"] = "buy", ["wore"] = "wear", ["worn"] = "wear", ["chose"] = "choose", ["chosen"] = "choose",
        ["ate"] = "eat", ["eaten"] = "eat", ["flew"] = "fly", ["flown"] = "fly", ["forgot"] = "forget",
        ["forgotten"] = "forget", ["hid"] = "hide", ["hidden"] = "hide", ["sang"] = "sing", ["sung"] = "sing",
        ["swam"] = "swim", ["threw"] = "throw", ["thrown"] = "throw", ["woke"] = "wake", ["won"] = "win",
        ["taught"] = "teach", ["caught"] = "catch", ["fought"] = "fight", ["sold"] = "sell", ["slept"] = "sleep",
    };

    // Слова, которые заканчиваются на s/us/ss/is, но не являются формой множественного числа.
    private static readonly HashSet<string> KeepAsIs = new(StringComparer.OrdinalIgnoreCase)
    {
        "this", "his", "its", "yes", "us", "bus", "gas", "plus", "was", "always", "perhaps", "across",
        "news", "series", "means", "species", "less", "unless", "business", "class", "glass", "pass", "boss",
        "address", "success", "process", "progress", "access", "guess", "dress", "kiss", "miss", "stress", "focus",
        "status", "virus", "bonus", "campus", "circus", "chorus", "analysis", "basis", "crisis", "thesis", "oasis",
    };

    [GeneratedRegex(@"[A-Za-z]+(?:'[A-Za-z]+)*")]
    private static partial Regex WordRegex();

    public IReadOnlyList<LemmaToken> Analyze(string text)
    {
        var result = new List<LemmaToken>();
        foreach (Match match in WordRegex().Matches(text))
        {
            result.Add(new LemmaToken(match.Value, LemmatizeWord(match.Value), match.Index, match.Length));
        }

        return result;
    }

    public string Lemmatize(string text) =>
        string.Join(' ', Analyze(text).Select(t => t.Lemma));

    public static string LemmatizeWord(string word)
    {
        var w = word.ToLowerInvariant();

        // Притяжательная форма и сокращения: dog's -> dog
        var apostrophe = w.IndexOf('\'');
        if (apostrophe > 0)
        {
            w = w[..apostrophe];
        }

        if (w.Length <= 2 || KeepAsIs.Contains(w))
        {
            return w;
        }

        if (Irregular.TryGetValue(w, out var irregular))
        {
            return irregular;
        }

        if (w.EndsWith("ies") && w.Length > 4)
        {
            return w[..^3] + "y";
        }

        if (w.EndsWith("sses") || w.EndsWith("shes") || w.EndsWith("ches") || w.EndsWith("xes") || w.EndsWith("zes"))
        {
            return w[..^2];
        }

        if (w.EndsWith("ied") && w.Length > 4)
        {
            return w[..^3] + "y";
        }

        if (w.EndsWith("ing") && w.Length > 5)
        {
            return RestoreStem(w[..^3]);
        }

        if (w.EndsWith("ed") && w.Length > 4)
        {
            return RestoreStem(w[..^2]);
        }

        if (w.EndsWith('s') && !w.EndsWith("ss") && !w.EndsWith("us") && !w.EndsWith("is") && w.Length > 3)
        {
            return w[..^1];
        }

        return w;
    }

    /// <summary>Восстанавливает основу после отбрасывания -ing/-ed: stopp -> stop, mak -> make.</summary>
    private static string RestoreStem(string stem)
    {
        // удвоенная согласная: stopp -> stop, running -> runn -> run
        if (stem.Length >= 3 && stem[^1] == stem[^2] && "bdgklmnprt".Contains(stem[^1]))
        {
            return stem[..^1];
        }

        // mak -> make, us -> use: если основа оканчивается на согласную после гласной, добавляем e
        if (stem.Length >= 3 && IsConsonant(stem[^1]) && !"wxy".Contains(stem[^1])
            && !IsConsonant(stem[^2]) && IsConsonant(stem[^3]) && stem[^1] != 'l' && stem[^1] != 'n' && stem[^1] != 'r')
        {
            return stem + "e";
        }

        return stem;
    }

    private static bool IsConsonant(char c) => char.IsLetter(c) && !"aeiou".Contains(c);
}
