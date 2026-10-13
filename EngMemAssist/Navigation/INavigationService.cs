using EngMemAssist.ViewModels;

namespace EngMemAssist.Navigation;

public interface INavigationService
{
    PageViewModelBase? Current { get; }

    void NavigateTo<TViewModel>() where TViewModel : PageViewModelBase;
}
