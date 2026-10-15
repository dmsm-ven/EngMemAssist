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
    private readonly List<QuizHint> _hints = [];
    private int _hintsUsed;
    private DateTime _startedAt;
    private bool _isFinishing;

    [ObservableProperty]
    private QuizQuestion? _current;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private IReadOnlyList<string> _visibleOptions = [];

    [ObservableProperty]
    private QuizHint _currentHint = QuizHint.None;

    [ObservableProperty]
    private QuizSentence? _extraSentence;

    [ObservableProperty]
    private string _firstLetterText = "";

    [ObservableProperty]
    private string _hintsText = "";

    [ObservableProperty]
    private bool _hasHints;

    public bool CanExtraContext => Current is { ExtraSentenceList.Count: > 0 };

    private int HintsTotal => QuizHintExtensions.Budget(_questions.Count);

    public bool HasExtraSentence => ExtraSentence is not null;

    public bool HasFirstLetter => FirstLetterText.Length > 0;

    partial void OnExtraSentenceChanged(QuizSentence? value) => OnPropertyChanged(nameof(HasExtraSentence));

    partial void OnFirstLetterTextChanged(string value) => OnPropertyChanged(nameof(HasFirstLetter));

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
        VisibleOptions = Current.Options;
        CurrentHint = QuizHint.None;
        ExtraSentence = null;
        FirstLetterText = "";
        OnPropertyChanged(nameof(CanExtraContext));
        RefreshHints();
    }

    private void RefreshHints()
    {
        HasHints = HintsTotal > 0;
        HintsText = $"Подсказки: осталось {HintsTotal - _hintsUsed} из {HintsTotal}";
        UseFiftyFiftyCommand.NotifyCanExecuteChanged();
        UseExtraContextCommand.NotifyCanExecuteChanged();
        UseFirstLetterCommand.NotifyCanExecuteChanged();
    }

    private bool CanUseHint() =>
        !_isFinishing && Current is not null && _hintsUsed < HintsTotal && CurrentHint == QuizHint.None;

    private bool TakeHint(QuizHint hint)
    {
        CurrentHint = hint;
        _hintsUsed++;
        RefreshHints();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanUseHint))]
    private void UseFiftyFifty()
    {
        if (Current is null)
        {
            return;
        }

        var wrong = Current.Options.Where(o => o != Current.CorrectOption).ToList();
        var toRemove = new HashSet<string>(wrong.OrderBy(_ => Random.Shared.Next()).Take((wrong.Count + 1) / 2));
        VisibleOptions = Current.Options.Where(o => !toRemove.Contains(o)).ToList();
        TakeHint(QuizHint.FiftyFifty);
    }

    private bool CanUseExtraContext() => CanUseHint() && CanExtraContext;

    [RelayCommand(CanExecute = nameof(CanUseExtraContext))]
    private void UseExtraContext()
    {
        if (Current is null || Current.ExtraSentenceList.Count == 0)
        {
            return;
        }

        ExtraSentence = Current.ExtraSentenceList[0];
        TakeHint(QuizHint.ExtraContext);
    }

    [RelayCommand(CanExecute = nameof(CanUseHint))]
    private void UseFirstLetter()
    {
        if (Current is null || Current.CorrectOption.Length == 0)
        {
            return;
        }

        FirstLetterText = $"Ответ начинается на «{char.ToUpperInvariant(Current.CorrectOption[0])}…» ({Current.CorrectOption.Length} букв)";
        TakeHint(QuizHint.FirstLetter);
    }

    [RelayCommand]
    private async Task SelectOptionAsync(string? option)
    {
        if (_isFinishing || Current is null)
        {
            return;
        }

        _answers.Add(option);
        _hints.Add(CurrentHint);

        if (_answers.Count < _questions.Count)
        {
            Show(_answers.Count);
            return;
        }

        _isFinishing = true;
        try
        {
            var result = await _quiz.FinishAsync(_questions, _answers, _hints, _startedAt);
            _navigation.NavigateTo<ResultsViewModel>(vm => vm.Result = result);
        }
        catch (Exception ex)
        {
            _isFinishing = false;
            _answers.RemoveAt(_answers.Count - 1);
            _hints.RemoveAt(_hints.Count - 1);
            MessageBox.Show($"Не удалось сохранить результаты:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => _navigation.NavigateTo<HomeViewModel>();
}
