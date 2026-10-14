using CommunityToolkit.Mvvm.ComponentModel;

namespace EngMemAssist.ViewModels;

public abstract class PageViewModelBase : ObservableObject
{
    public abstract string Title { get; }

    /// <summary>Вызывается после показа страницы: здесь загружаются данные. Исключения показываются пользователю.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    /// <summary>Вызывается при уходе со страницы: здесь отписываемся от событий долгоживущих сервисов.</summary>
    public virtual void OnNavigatedFrom()
    {
    }
}
