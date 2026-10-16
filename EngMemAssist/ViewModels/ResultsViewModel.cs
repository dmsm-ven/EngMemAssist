using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Services;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

/// <summary>Результаты теста: сразу после прохождения или из истории на странице статистики.</summary>
public sealed partial class ResultsViewModel : PageViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ISoundService _sound;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private QuizResult? _result;

    /// <summary>Подпись под заголовком, например «Тест от 14.10.2026 12:30» для истории.</summary>
    [ObservableProperty]
    private string _subtitle = "";

    /// <summary>Результаты открыты из истории: показывается кнопка возврата к статистике.</summary>
    [ObservableProperty]
    private bool _isHistory;

    public ResultsViewModel(INavigationService navigation, ISoundService sound)
    {
        _sound = sound;
        _navigation = navigation;
    }

    /// <summary>Итог теста в виде рассказа.</summary>
    [ObservableProperty]
    private QuizStory? _story;

    partial void OnResultChanged(QuizResult? value) => Story = value is null ? null : QuizStoryBuilder.Build(value);

    /// <summary>Конфетти — только сразу после теста, пройденного без единой ошибки.</summary>
    public bool ShowConfetti => !IsHistory && Result is { Total: > 0 } r && r.CorrectCount == r.Total;

    public override Task OnNavigatedToAsync()
    {
        if (ShowConfetti)
        {
            _sound.Play(SoundEffect.Fanfare);
        }

        return Task.CompletedTask;
    }

    public override string Title => "Результаты";

    /// <summary>Результаты из истории относятся к «Статистике», результаты только что пройденного теста — к «Главной».</summary>
    public override NavSection Section => IsHistory ? NavSection.Stats : NavSection.Home;

    public string SummaryText => Result is null ? "" : $"Верно: {Result.CorrectCount} из {Result.Total}";

    [RelayCommand]
    private void Again() => _navigation.NavigateTo<QuizViewModel>();

    [RelayCommand]
    private void Home() => _navigation.NavigateTo<HomeViewModel>();

    [RelayCommand]
    private void BackToStats() => _navigation.NavigateTo<StatsViewModel>();
}
