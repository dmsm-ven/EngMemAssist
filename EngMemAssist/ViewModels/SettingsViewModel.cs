using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Settings;
using EngMemAssist.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EngMemAssist.ViewModels;

public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly ISettingsService _settings;
    private bool _loading = true;

    [ObservableProperty]
    private string _spreadsheetUrl = "";

    [ObservableProperty]
    private int _questionCount;

    [ObservableProperty]
    private string _theme = "Dark";

    public SettingsViewModel(ISettingsService settings, SyncCoordinator sync)
    {
        _settings = settings;
        Sync = sync;

        SpreadsheetUrl = settings.Current.SpreadsheetUrl ?? "";
        QuestionCount = settings.Current.QuizQuestionCount;
        Theme = settings.Current.Theme;
        _loading = false;
    }

    public override string Title => "Настройки";

    public SyncCoordinator Sync { get; }

    public int[] QuestionCountOptions { get; } = [5, 10, 15, 20, 30];

    public string[] ThemeOptions => ThemeHelper.Themes;

    public string ClientSecretPath => AppPaths.ClientSecretFile;

    partial void OnQuestionCountChanged(int value)
    {
        if (_loading)
        {
            return;
        }

        _settings.Current.QuizQuestionCount = value;
        _settings.Save();
    }

    partial void OnThemeChanged(string value)
    {
        if (_loading)
        {
            return;
        }

        _settings.Current.Theme = value;
        _settings.Save();
        ThemeHelper.Apply(value);
    }

    [RelayCommand]
    private async Task ChangeSpreadsheetAsync()
    {
        await Sync.ConnectAndSyncAsync();
        SpreadsheetUrl = _settings.Current.SpreadsheetUrl ?? "";
    }

    [RelayCommand]
    private void SignOutGoogle()
    {
        try
        {
            if (Directory.Exists(AppPaths.GoogleTokenDirectory))
            {
                Directory.Delete(AppPaths.GoogleTokenDirectory, recursive: true);
            }

            MessageBox.Show("Вход в Google сброшен. При следующей синхронизации откроется окно входа.",
                "EngMemAssist", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сбросить вход:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        AppPaths.EnsureCreated();
        Process.Start(new ProcessStartInfo(AppPaths.Root) { UseShellExecute = true });
    }
}
