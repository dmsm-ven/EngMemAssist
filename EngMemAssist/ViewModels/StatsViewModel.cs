using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class StatsViewModel : PageViewModelBase
{
    private readonly IQuizRepository _quizzes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private StatsOverview _overview = new();

    [ObservableProperty]
    private IReadOnlyList<WordDifficulty> _hardestWords = [];

    [ObservableProperty]
    private IReadOnlyList<WordDifficulty> _wordStats = [];

    [ObservableProperty]
    private IReadOnlyList<StageSummary> _stageSummaries = [];

    [ObservableProperty]
    private IReadOnlyList<HeatmapWeek> _heatmapWeeks = [];

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _activityText = "";

    public StatsViewModel(IQuizRepository quizzes)
    {
        _quizzes = quizzes;
    }

    public override string Title => "Статистика";

    public override NavSection Section => NavSection.Stats;

    public override async Task OnNavigatedToAsync()
    {
        IsLoading = true;
        try
        {
            Overview = await _quizzes.GetOverviewAsync();
            WordStats = await _quizzes.GetWordStatsAsync();
            HardestWords = await _quizzes.GetHardestWordsAsync(15);
            BuildProgress();
            await BuildHeatmapAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Сколько слов выучено и как слова распределены по стадиям.</summary>
    private void BuildProgress()
    {
        var total = WordStats.Count;
        var byStage = WordStats.GroupBy(w => w.Stage).ToDictionary(g => g.Key, g => g.Count());

        StageSummaries = WordStages.All.Select(stage => new StageSummary(stage, byStage.GetValueOrDefault(stage))).ToList();

        var learned = byStage.GetValueOrDefault(WordStage.Learned);
        ProgressPercent = total == 0 ? 0 : 100.0 * learned / total;
        ProgressText = total == 0
            ? "В словаре пока нет слов"
            : $"Выучено {learned} из {total} слов ({ProgressPercent:0}%)";
    }

    /// <summary>Карта активности за последние недели: колонка — неделя, строка — день недели.</summary>
    private async Task BuildHeatmapAsync()
    {
        var activity = await _quizzes.GetDailyActivityAsync(HeatmapWeeksCount * 7 + 7);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var mondayOffset = ((int)today.DayOfWeek + 6) % 7;
        var start = today.AddDays(-mondayOffset - 7 * (HeatmapWeeksCount - 1));

        var weeks = new List<HeatmapWeek>();
        var answers = 0;
        var activeDays = 0;

        for (var w = 0; w < HeatmapWeeksCount; w++)
        {
            var days = new List<HeatmapDay>();
            for (var d = 0; d < 7; d++)
            {
                var date = start.AddDays(w * 7 + d);
                var count = date > today ? 0 : activity.GetValueOrDefault(date);
                if (count > 0)
                {
                    answers += count;
                    activeDays++;
                }

                days.Add(new HeatmapDay(date, count, date > today));
            }

            weeks.Add(new HeatmapWeek(days));
        }

        HeatmapWeeks = weeks;
        ActivityText = answers == 0
            ? $"За последние {HeatmapWeeksCount} недель ответов пока нет"
            : $"За последние {HeatmapWeeksCount} недель: ответов — {answers}, активных дней — {activeDays}";
    }

    private const int HeatmapWeeksCount = 24;
}
