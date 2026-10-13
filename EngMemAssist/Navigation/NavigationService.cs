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

    public void NavigateTo<TViewModel>() where TViewModel : PageViewModelBase
    {
        Current = _serviceProvider.GetRequiredService<TViewModel>();
    }
}
