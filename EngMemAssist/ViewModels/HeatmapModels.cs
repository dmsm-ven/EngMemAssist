using System.Globalization;

namespace EngMemAssist.ViewModels;

/// <summary>Один день на карте активности. Level: 0 — нет ответов, 1–4 — от меньшего числа ответов к большему.</summary>
public sealed record HeatmapDay(DateOnly Date, int Count, bool IsFuture)
{
    public int Level => Count switch
    {
        0 => 0,
        <= 5 => 1,
        <= 10 => 2,
        <= 20 => 3,
        _ => 4,
    };

    /// <summary>Непрозрачность акцентного цвета для уровня (пустые дни рисуются серым через триггер в XAML).</summary>
    public double CellOpacity => Level switch
    {
        1 => 0.35,
        2 => 0.55,
        3 => 0.78,
        4 => 1.0,
        _ => 1.0,
    };

    public string Tooltip
    {
        get
        {
            var date = Date.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
            return Count == 0 ? $"{date}: нет ответов" : $"{date}: ответов — {Count}";
        }
    }
}

/// <summary>Неделя (колонка карты активности): 7 дней с понедельника по воскресенье.</summary>
public sealed record HeatmapWeek(IReadOnlyList<HeatmapDay> Days);

/// <summary>Сколько слов находится на стадии изучения (для полосы прогресса).</summary>
public sealed record StageSummary(Core.Models.WordStage Stage, int Count)
{
    public string Title => $"{Stage.Icon()} {Stage.Title()}";

    public string Description => Stage.Description();
}
