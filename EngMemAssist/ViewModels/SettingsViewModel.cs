using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Settings;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;
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

    [ObservableProperty]
    private double _backgroundOpacityPercent;

    [ObservableProperty]
    private string _backgroundOverlayColor = "#000000";

    public SettingsViewModel(ISettingsService settings, SyncCoordinator sync, AppBackgroundService background)
    {
        _settings = settings;
        Sync = sync;
        Background = background;
        BackgroundOpacityPercent = Math.Round(settings.Current.BackgroundOpacity * 100);
        BackgroundOverlayColor = settings.Current.BackgroundOverlayColor;

        SpreadsheetUrl = settings.Current.SpreadsheetUrl ?? "";
        QuestionCount = settings.Current.QuizQuestionCount;
        Theme = settings.Current.Theme;
        _loading = false;
    }

    public override string Title => "Настройки";

    public override NavSection Section => NavSection.Settings;

    public SyncCoordinator Sync { get; }

    public AppBackgroundService Background { get; }

    public ColorPreset[] OverlayColors => AppBackgroundService.Presets;

    partial void OnBackgroundOpacityPercentChanged(double value)
    {
        if (!_loading)
        {
            Background.SetOpacity(value / 100.0);
        }
    }

    partial void OnBackgroundOverlayColorChanged(string value)
    {
        if (!_loading && !string.IsNullOrEmpty(value))
        {
            Background.SetOverlayColor(value);
        }
    }

    [RelayCommand]
    private void ChooseBackground()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Фоновое изображение",
            Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            Background.SetImage(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось установить фон:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void ClearBackground() => Background.ClearImage();

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
