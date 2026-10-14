using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class QuizViewModel : PageViewModelBase
{
    private readonly IQuizService _quiz;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigation;

    private List<QuizQuestion> _questions = [];
    private readonly List<string?> _answers = [];
    private DateTime _startedAt;
    private bool _isFinishing;

    [ObservableProperty]
    private QuizQuestion? _current;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private bool _isLoading = true;

    public QuizViewModel(IQuizService quiz, ISettingsService settings, INavigationService navigation)
    {
        _quiz = quiz;
        _settings = settings;
        _navigation = navigation;
    }

    public override string Title => "Квиз";

    public override async Task OnNavigatedToAsync()
    {
        try
        {
            var questions = await _quiz.CreateQuizAsync(
                _settings.Current.QuizQuestionCount,
                _settings.Current.QuizOptionsCount);
            _questions = questions.ToList();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "EngMemAssist", MessageBoxButton.OK, MessageBoxImage.Information);
            _navigation.NavigateTo<HomeViewModel>();
            return;
        }

        _startedAt = DateTime.UtcNow;
        Show(0);
        IsLoading = false;
    }

    private void Show(int index)
    {
        Current = _questions[index];
        ProgressText = $"Вопрос {index + 1} из {_questions.Count}";
    }

    [RelayCommand]
    private async Task SelectOptionAsync(string? option)
    {
        if (_isFinishing || Current is null)
        {
            return;
        }

        _answers.Add(option);

        if (_answers.Count < _questions.Count)
        {
            Show(_answers.Count);
            return;
        }

        _isFinishing = true;
        try
        {
            var result = await _quiz.FinishAsync(_questions, _answers, _startedAt);
            _navigation.NavigateTo<ResultsViewModel>(vm => vm.Result = result);
        }
        catch (Exception ex)
        {
            _isFinishing = false;
            _answers.RemoveAt(_answers.Count - 1);
            MessageBox.Show($"Не удалось сохранить результаты:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => _navigation.NavigateTo<HomeViewModel>();
}
