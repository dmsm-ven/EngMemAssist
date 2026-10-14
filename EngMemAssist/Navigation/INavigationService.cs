using System.ComponentModel;
using EngMemAssist.ViewModels;

namespace EngMemAssist.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    PageViewModelBase? Current { get; }

    /// <summary>Открывает страницу. configure позволяет передать данные во ViewModel до показа.</summary>
    void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : PageViewModelBase;
}
