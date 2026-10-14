using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class StatsViewModel : PageViewModelBase
{
    private readonly IQuizRepository _quizzes;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private StatsOverview _overview = new();

    [ObservableProperty]
    private IReadOnlyList<SessionSummary> _sessions = [];

    [ObservableProperty]
    private IReadOnlyList<WordDifficulty> _hardestWords = [];

    [ObservableProperty]
    private SessionSummary? _selectedSession;

    public StatsViewModel(IQuizRepository quizzes, INavigationService navigation)
    {
        _quizzes = quizzes;
        _navigation = navigation;
    }

    public override string Title => "Статистика";

    public override NavSection Section => NavSection.Stats;

    public override async Task OnNavigatedToAsync()
    {
        Overview = await _quizzes.GetOverviewAsync();
        Sessions = await _quizzes.GetRecentSessionsAsync(20);
        HardestWords = await _quizzes.GetHardestWordsAsync(15);
    }

    /// <summary>Выбор теста в списке открывает его результаты, как сразу после прохождения.</summary>
    partial void OnSelectedSessionChanged(SessionSummary? value)
    {
        if (value is not null)
        {
            _ = OpenSessionAsync(value);
        }
    }

    private async Task OpenSessionAsync(SessionSummary session)
    {
        try
        {
            var result = await _quizzes.GetSessionResultAsync(session.Id);
            _navigation.NavigateTo<ResultsViewModel>(vm =>
            {
                vm.Result = result;
                vm.Subtitle = $"Тест от {session.StartedAtLocal}";
                vm.IsHistory = true;
            });
        }
        catch (Exception ex)
        {
            SelectedSession = null;
            MessageBox.Show($"Не удалось открыть результаты теста:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
