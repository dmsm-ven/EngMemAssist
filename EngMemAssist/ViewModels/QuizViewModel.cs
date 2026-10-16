using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Models;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;
using System.Windows;

namespace EngMemAssist.ViewModels;

public sealed partial class QuizViewModel : PageViewModelBase
{
    private readonly IQuizService _quiz;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigation;
    private readonly ISoundService _sound;

    private List<QuizQuestion> _questions = [];
    private readonly List<string?> _answers = [];
    private readonly List<QuizHint> _hints = [];
    private int _hintsUsed;
    private DateTime _startedAt;
    private bool _isFinishing;
    private bool _isAnswering;
    private bool _cancelled;

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
    private string _hintsText = "";

    [ObservableProperty]
    private bool _hasHints;

    public bool CanExtraContext => Current is { ExtraSentenceList.Count: > 0 };

    private int HintsTotal => QuizHintExtensions.Budget(_questions.Count);

    public bool HasExtraSentence => ExtraSentence is not null;

    partial void OnExtraSentenceChanged(QuizSentence? value) => OnPropertyChanged(nameof(HasExtraSentence));


    /// <summary>Неверный ответ: страница должна встряхнуться (подписывается View).</summary>
    public event EventHandler? ShakeRequested;

    /// <summary>Сразу перед сменой вопроса: View делает снимок текущего вопроса для анимации «уезжания».</summary>
    public event EventHandler? QuestionChanging;

    public QuizViewModel(
        IQuizService quiz, ISettingsService settings, INavigationService navigation, ISoundService sound)
    {
        _sound = sound;
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
                _settings.Current.QuizOptionsCount,
                _settings.Current.QuizStrategy);
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
        OnPropertyChanged(nameof(CanExtraContext));
        RefreshHints();
    }

    private void RefreshHints()
    {
        HasHints = HintsTotal > 0;
        HintsText = $"Подсказки: осталось {HintsTotal - _hintsUsed} из {HintsTotal}";
        UseFiftyFiftyCommand.NotifyCanExecuteChanged();
        UseExtraContextCommand.NotifyCanExecuteChanged();
    }

    private bool CanUseHint() =>
        !_isFinishing && !_isAnswering && Current is not null && _hintsUsed < HintsTotal && CurrentHint == QuizHint.None;

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

    [RelayCommand]
    private async Task SelectOptionAsync(string? option)
    {
        if (_isFinishing || _isAnswering || Current is null)
        {
            return;
        }

        // Короткая пауза с обратной связью: звук и (при ошибке) встряска, затем следующий вопрос.
        _isAnswering = true;
        var correct = Current.IsCorrect(option);
        _sound.Play(correct ? SoundEffect.Correct : SoundEffect.Wrong);

        if (!correct && _settings.Current.ShakeOnWrong)
        {
            ShakeRequested?.Invoke(this, EventArgs.Empty);
            await Task.Delay(450);
        }
        else
        {
            await Task.Delay(220);
        }

        _isAnswering = false;
        if (_cancelled)
        {
            return;
        }

        _answers.Add(option);
        _hints.Add(CurrentHint);

        if (_answers.Count < _questions.Count)
        {
            QuestionChanging?.Invoke(this, EventArgs.Empty);
            Show(_answers.Count);
            //_sound.Play(SoundEffect.Next);
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
    private void Cancel()
    {
        _cancelled = true;
        _navigation.NavigateTo<HomeViewModel>();
    }
}
