using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class HomeViewModel : PageViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IWordRepository _words;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartQuizCommand))]
    private int _wordCount;

    [ObservableProperty]
    private string _summary = "";

    public HomeViewModel(INavigationService navigation, SyncCoordinator sync, IWordRepository words)
    {
        _navigation = navigation;
        _words = words;
        Sync = sync;
    }

    public override string Title => "Главная";

    public SyncCoordinator Sync { get; }

    public override async Task OnNavigatedToAsync()
    {
        Sync.PropertyChanged += OnSyncPropertyChanged;
        Sync.Completed += OnSyncCompleted;
        await RefreshAsync();
    }

    public override void OnNavigatedFrom()
    {
        Sync.PropertyChanged -= OnSyncPropertyChanged;
        Sync.Completed -= OnSyncCompleted;
    }

    private void OnSyncPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncCoordinator.IsBusy))
        {
            StartQuizCommand.NotifyCanExecuteChanged();
            SyncCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
        }
    }

    private async void OnSyncCompleted(object? sender, EventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch
        {
            // Обновление счётчика не критично.
        }
    }

    private async Task RefreshAsync()
    {
        var words = await _words.GetActiveAsync();
        WordCount = words.Count;
        Summary = WordCount == 0 ? "В словаре пока нет слов." : $"Слов в словаре: {WordCount}";
    }

    private bool CanStartQuiz() => !Sync.IsBusy && WordCount > 0;

    private bool CanSync() => !Sync.IsBusy;

    [RelayCommand(CanExecute = nameof(CanStartQuiz))]
    private void StartQuiz() => _navigation.NavigateTo<QuizViewModel>();

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncAsync() => await Sync.SyncAsync();

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task ConnectAsync() => await Sync.ConnectAndSyncAsync();
}
