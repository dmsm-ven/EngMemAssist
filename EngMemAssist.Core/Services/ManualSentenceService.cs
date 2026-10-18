using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.Core.Services;

/// <summary>Добавление предложения для слова вручную (для редких слов, по которым источники ничего не нашли).</summary>
public sealed class ManualSentenceService
{
    private readonly ILemmatizer _lemmatizer;
    private readonly ISentenceRepository _sentences;

    public ManualSentenceService(ILemmatizer lemmatizer, ISentenceRepository sentences)
    {
        _lemmatizer = lemmatizer;
        _sentences = sentences;
    }

    /// <summary>
    /// Проверяет, что в предложении есть слово (в любой форме), и сохраняет его с источником «Введено пользователем».
    /// Бросает <see cref="InvalidOperationException"/> с понятным текстом, если предложение не подходит.
    /// </summary>
    public async Task AddAsync(Word word, string? text)
    {
        var clean = string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (clean.Length == 0)
        {
            throw new InvalidOperationException("Введите предложение.");
        }

        var pattern = WordPattern.Create(_lemmatizer, word.English);
        var tokens = await Task.Run(() => _lemmatizer.Analyze(clean).ToList());

        if (!SentenceMatcher.TryFind(tokens, pattern, out var start, out var length))
        {
            throw new InvalidOperationException(
                $"В предложении не найдено слово «{word.English}» (в любой форме). Проверьте написание.");
        }

        var added = await _sentences.AddRangeAsync(
        [
            new Sentence
            {
                WordId = word.Id,
                Text = clean,
                Source = Sentence.UserSourceTag,
                MatchStart = start,
                MatchLength = length,
            },
        ]);

        if (added == 0)
        {
            throw new InvalidOperationException("Такое предложение для этого слова уже есть.");
        }
    }
}
