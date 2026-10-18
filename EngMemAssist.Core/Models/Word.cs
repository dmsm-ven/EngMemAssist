namespace EngMemAssist.Core.Models;

/// <summary>Слово из таблицы-источника, сохранённое в локальной БД.</summary>
public sealed class Word : System.ComponentModel.INotifyPropertyChanged
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

    /// <summary>Сколько проверочных предложений загружено (не хранится в Words, заполняется при показе списка).</summary>
    public int SentenceCount
    {
        get => _sentenceCount;
        set
        {
            if (_sentenceCount != value)
            {
                _sentenceCount = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(SentenceCount)));
            }
        }
    }

    private int _sentenceCount;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Отдельные значения перевода (любое из них считается верным).</summary>
    public IReadOnlyList<string> GetTranslations() =>
        Translation.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
