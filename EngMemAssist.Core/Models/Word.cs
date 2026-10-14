namespace EngMemAssist.Core.Models;

/// <summary>Слово из таблицы-источника, сохранённое в локальной БД.</summary>
public sealed class Word
{
    public long Id { get; set; }

    /// <summary>Нормальная форма слова (child для children). Уникальна, используется для поиска дублей.</summary>
    public string Lemma { get; set; } = "";

    /// <summary>Слово так, как записано в таблице.</summary>
    public string English { get; set; } = "";

    /// <summary>Переводы через запятую, как в таблице: "унылый, скучный".</summary>
    public string Translation { get; set; } = "";

    public DateTime AddedAt { get; set; }

    /// <summary>Слово пропало из таблицы: в квизах не участвует, статистика сохраняется.</summary>
    public bool IsRemoved { get; set; }

    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public DateTime? LastAskedAt { get; set; }

    /// <summary>Когда успешно запрашивали предложения. Null — предложения ещё не загружены (или была ошибка).</summary>
    public DateTime? SentencesFetchedAt { get; set; }

    /// <summary>Отдельные значения перевода (любое из них считается верным).</summary>
    public IReadOnlyList<string> GetTranslations() =>
        Translation.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
