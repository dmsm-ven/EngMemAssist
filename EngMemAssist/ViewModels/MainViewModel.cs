using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Navigation;

namespace EngMemAssist.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public MainViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    /// <summary>Сервис навигации; Content окна привязан к его Current.</summary>
    public INavigationService Navigation => _navigation;

    [RelayCommand]
    private void ShowHome() => _navigation.NavigateTo<HomeViewModel>();

    [RelayCommand]
    private void ShowWords() => _navigation.NavigateTo<WordsViewModel>();

    [RelayCommand]
    private void ShowStats() => _navigation.NavigateTo<StatsViewModel>();

    [RelayCommand]
    private void ShowSettings() => _navigation.NavigateTo<SettingsViewModel>();
}
