using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Navigation;

/// <summary>Показывает текущую страницу в окне: ViewModel берётся из DI, View подбирается через DataTemplate.</summary>
public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private PageViewModelBase? _current;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : PageViewModelBase
    {
        Current?.OnNavigatedFrom();

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        configure?.Invoke(viewModel);
        Current = viewModel;

        _ = RunNavigatedAsync(viewModel);
    }

    private static async Task RunNavigatedAsync(PageViewModelBase viewModel)
    {
        try
        {
            await viewModel.OnNavigatedToAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии страницы «{viewModel.Title}»:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
