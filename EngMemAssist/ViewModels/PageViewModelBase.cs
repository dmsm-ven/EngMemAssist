using CommunityToolkit.Mvvm.ComponentModel;

namespace EngMemAssist.ViewModels;

public abstract class PageViewModelBase : ObservableObject
{
    public abstract string Title { get; }
}
