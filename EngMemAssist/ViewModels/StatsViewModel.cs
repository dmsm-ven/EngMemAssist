using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;

namespace EngMemAssist.ViewModels;

public sealed partial class StatsViewModel : PageViewModelBase
{
    private readonly IQuizRepository _quizzes;

    [ObservableProperty]
    private StatsOverview _overview = new();

    [ObservableProperty]
    private IReadOnlyList<SessionSummary> _sessions = [];

    [ObservableProperty]
    private IReadOnlyList<WordDifficulty> _hardestWords = [];

    public StatsViewModel(IQuizRepository quizzes)
    {
        _quizzes = quizzes;
    }

    public override string Title => "Статистика";

    public override async Task OnNavigatedToAsync()
    {
        Overview = await _quizzes.GetOverviewAsync();
        Sessions = await _quizzes.GetRecentSessionsAsync(20);
        HardestWords = await _quizzes.GetHardestWordsAsync(15);
    }
}
