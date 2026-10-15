using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public MainViewModel(INavigationService navigation, BackgroundService background)
    {
        _navigation = navigation;
        Background = background;
        _navigation.PropertyChanged += OnNavigationChanged;
    }

    /// <summary>Фон окна (картинка и подложка).</summary>
    public BackgroundService Background { get; }

    /// <summary>Сервис навигации; Content окна привязан к его Current.</summary>
    public INavigationService Navigation => _navigation;

    public bool IsHomeActive => ActiveSection == NavSection.Home;

    public bool IsWordsActive => ActiveSection == NavSection.Words;

    public bool IsStatsActive => ActiveSection == NavSection.Stats;

    public bool IsSettingsActive => ActiveSection == NavSection.Settings;

    private NavSection ActiveSection => _navigation.Current?.Section ?? NavSection.Home;

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(INavigationService.Current))
        {
            return;
        }

        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsWordsActive));
        OnPropertyChanged(nameof(IsStatsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    [RelayCommand]
    private void ShowHome() => _navigation.NavigateTo<HomeViewModel>();

    [RelayCommand]
    private void ShowWords() => _navigation.NavigateTo<WordsViewModel>();

    [RelayCommand]
    private void ShowStats() => _navigation.NavigateTo<StatsViewModel>();

    [RelayCommand]
    private void ShowSettings() => _navigation.NavigateTo<SettingsViewModel>();
}
