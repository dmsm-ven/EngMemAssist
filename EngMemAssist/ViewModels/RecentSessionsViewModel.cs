using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Models;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

/// <summary>Список последних тестов; общий для страницы статистики и главной. Выбор теста открывает его результаты.</summary>
public sealed partial class RecentSessionsViewModel : ObservableObject
{
    private readonly IQuizRepository _quizzes;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private IReadOnlyList<SessionSummary> _sessions = [];

    [ObservableProperty]
    private SessionSummary? _selectedSession;

    public RecentSessionsViewModel(IQuizRepository quizzes, INavigationService navigation)
    {
        _quizzes = quizzes;
        _navigation = navigation;
    }

    public async Task LoadAsync(int take = 20) => Sessions = await _quizzes.GetRecentSessionsAsync(take);

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
