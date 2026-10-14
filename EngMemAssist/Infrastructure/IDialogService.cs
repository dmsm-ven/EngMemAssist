using System.Windows;
using EngMemAssist.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngMemAssist.Infrastructure;

public interface IDialogService
{
    /// <summary>Показывает окно подключения таблицы. true — таблица подключена и сохранена в настройках.</summary>
    bool? ShowConnectSpreadsheet();
}

public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool? ShowConnectSpreadsheet()
    {
        var window = _serviceProvider.GetRequiredService<ConnectSpreadsheetWindow>();
        var owner = Application.Current.MainWindow;
        if (owner is { IsLoaded: true })
        {
            window.Owner = owner;
        }

        return window.ShowDialog();
    }
}
