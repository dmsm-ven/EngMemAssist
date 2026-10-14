using Catalyst;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Settings;
using Mosaik.Core;

namespace EngMemAssist.Nlp;

/// <summary>
/// Лемматизатор на Catalyst (английская модель). Модели грузятся лениво при первом обращении
/// (первый запуск долгий: модели распаковываются в %AppData%/EngMemAssist/catalyst-models).
/// Если модели загрузить не удалось, работает запасной лемматизатор на правилах, причина доступна в DegradedReason.
/// </summary>
public sealed class CatalystLemmatizer : ILemmatizer, ILemmatizerDiagnostics
{
    private readonly object _sync = new();
    private readonly RuleBasedLemmatizer _fallback = new();
    private readonly string _modelsDirectory;

    private Pipeline? _pipeline;
    private bool _initialized;

    public CatalystLemmatizer(string? modelsDirectory = null)
    {
        _modelsDirectory = modelsDirectory ?? AppPaths.NlpModelsDirectory;
    }

    public string? DegradedReason { get; private set; }

    public IReadOnlyList<LemmaToken> Analyze(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        lock (_sync)
        {
            EnsureInitialized();
            if (_pipeline is null)
            {
                return _fallback.Analyze(text);
            }

            try
            {
                return AnalyzeWithCatalyst(_pipeline, text);
            }
            catch (Exception ex)
            {
                DegradedReason = $"Ошибка Catalyst: {ex.Message}";
                return _fallback.Analyze(text);
            }
        }
    }

    public string Lemmatize(string text) =>
        string.Join(' ', Analyze(text).Select(t => t.Lemma));

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            Directory.CreateDirectory(_modelsDirectory);
            Catalyst.Models.English.Register();
            Storage.Current = new DiskStorage(_modelsDirectory);
            _pipeline = Pipeline.ForAsync(Language.English).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _pipeline = null;
            DegradedReason = $"Не удалось загрузить модели Catalyst: {ex.Message}";
        }
    }

    private static List<LemmaToken> AnalyzeWithCatalyst(Pipeline pipeline, string text)
    {
        var document = new Document(text, Language.English);
        pipeline.ProcessSingle(document);

        var result = new List<LemmaToken>();
        var cursor = 0;

        foreach (var span in document)
        {
            foreach (var token in span)
            {
                var value = token.Value;
                if (string.IsNullOrEmpty(value) || !value.Any(char.IsLetter))
                {
                    continue;
                }

                // Позицию определяем поиском значения токена в исходной строке: не зависит от смещений внутри Catalyst.
                var index = text.IndexOf(value, cursor, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    continue;
                }

                cursor = index + value.Length;

                var lemma = string.IsNullOrWhiteSpace(token.Lemma) ? value : token.Lemma;
                result.Add(new LemmaToken(value, lemma.ToLowerInvariant(), index, value.Length));
            }
        }

        return result;
    }
}
