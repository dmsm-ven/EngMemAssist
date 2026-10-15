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

    public StatsViewModel(IQuizRepository quizzes, RecentSessionsViewModel recentSessions)
    {
        _quizzes = quizzes;
        RecentSessions = recentSessions;
    }

    public RecentSessionsViewModel RecentSessions { get; }

    public override string Title => "Статистика";

    public override NavSection Section => NavSection.Stats;

    public override async Task OnNavigatedToAsync()
    {
        IsLoading = true;
        try
        {
            Overview = await _quizzes.GetOverviewAsync();
            await RecentSessions.LoadAsync();
            WordStats = await _quizzes.GetWordStatsAsync();
            HardestWords = await _quizzes.GetHardestWordsAsync(15);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
