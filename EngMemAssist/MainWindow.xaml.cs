using EngMemAssist.Core.Settings;
using EngMemAssist.Infrastructure;
using EngMemAssist.ViewModels;
using System.ComponentModel;
using Wpf.Ui.Controls;

namespace EngMemAssist;

public partial class MainWindow : FluentWindow
{
    private readonly ISettingsService _settings;
    private readonly AppShell _shell;

    public MainWindow(MainViewModel viewModel, ISettingsService settings, AppShell shell)
    {
        _settings = settings;
        _shell = shell;
        DataContext = viewModel;
        InitializeComponent();
    }

    /// <summary>При включённых напоминаниях закрытие окна сворачивает приложение в трей; выход — через меню значка.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_settings.Current.RemindersEnabled && !_shell.IsExiting)
        {
            e.Cancel = true;
            Hide();
            _shell.ShowTrayHint();
            return;
        }

        base.OnClosing(e);
    }
}
