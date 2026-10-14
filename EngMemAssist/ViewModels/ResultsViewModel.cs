using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Models;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

/// <summary>Результаты теста: сразу после прохождения или из истории на странице статистики.</summary>
public sealed partial class ResultsViewModel : PageViewModelBase
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private QuizResult? _result;

    /// <summary>Подпись под заголовком, например «Тест от 14.10.2026 12:30» для истории.</summary>
    [ObservableProperty]
    private string _subtitle = "";

    /// <summary>Результаты открыты из истории: показывается кнопка возврата к статистике.</summary>
    [ObservableProperty]
    private bool _isHistory;

    public ResultsViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public override string Title => "Результаты";

    public string SummaryText => Result is null ? "" : $"Верно: {Result.CorrectCount} из {Result.Total}";

    [RelayCommand]
    private void Again() => _navigation.NavigateTo<QuizViewModel>();

    [RelayCommand]
    private void Home() => _navigation.NavigateTo<HomeViewModel>();

    [RelayCommand]
    private void BackToStats() => _navigation.NavigateTo<StatsViewModel>();
}
