using EngMemAssist.Core.Settings;
using EngMemAssist.Navigation;
using EngMemAssist.ViewModels;
using System.Windows;
using Forms = System.Windows.Forms;

namespace EngMemAssist.Infrastructure;

/// <summary>Оболочка приложения: значок в трее, показ окна, мини-тест и выход.</summary>
public sealed class AppShell : IDisposable
{
    /// <summary>Сколько вопросов в мини-тесте из напоминания.</summary>
    public const int QuickQuizQuestions = 5;

    private readonly ISettingsService _settings;
    private readonly INavigationService _navigation;
    private Forms.NotifyIcon? _icon;
    private bool _hintShown;

    public AppShell(ISettingsService settings, INavigationService navigation)
    {
        _settings = settings;
        _navigation = navigation;
    }

    /// <summary>true — идёт настоящий выход, закрытие окна не нужно превращать в сворачивание в трей.</summary>
    public bool IsExiting { get; private set; }

    public void Initialize()
    {
        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico")).Stream;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Открыть", null, (_, _) => ShowMainWindow());
        menu.Items.Add($"Мини-тест ({QuickQuizQuestions} вопросов)", null, (_, _) => StartQuickQuiz());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => Exit());

        _icon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(stream),
            Text = "EngMemAssist",
            ContextMenuStrip = menu,
            Visible = _settings.Current.RemindersEnabled,
        };
        _icon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>Значок виден только пока включены напоминания.</summary>
    public void RefreshTray()
    {
        if (_icon is not null)
        {
            _icon.Visible = _settings.Current.RemindersEnabled;
        }
    }

    /// <summary>Однократная подсказка при первом сворачивании в трей за сессию.</summary>
    public void ShowTrayHint()
    {
        if (_hintShown || _icon is null)
        {
            return;
        }

        _hintShown = true;
        _icon.ShowBalloonTip(3000, "EngMemAssist", "Приложение работает в трее и напомнит о мини-тесте. Выход — через меню значка.", Forms.ToolTipIcon.Info);
    }

    public void ShowMainWindow()
    {
        var window = Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
    }

    public void StartQuickQuiz()
    {
        ShowMainWindow();
        _navigation.NavigateTo<QuizViewModel>(vm => vm.QuestionCountOverride = QuickQuizQuestions);
    }

    public void Exit()
    {
        IsExiting = true;
        Dispose();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_icon is null)
        {
            return;
        }

        _icon.Visible = false;
        _icon.Dispose();
        _icon = null;
    }
}
