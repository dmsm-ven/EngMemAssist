using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Models;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class ResultsViewModel : PageViewModelBase
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private QuizResult? _result;

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
}
