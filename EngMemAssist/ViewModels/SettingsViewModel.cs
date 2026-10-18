using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using EngMemAssist.Infrastructure;
using EngMemAssist.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EngMemAssist.ViewModels;

public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ILocalSourceService _localSources;
    private readonly ReminderService _reminders;
    private readonly AppShell _shell;
    private bool _loading = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _remindersEnabled;

    [ObservableProperty]
    private int _reminderIntervalMinutes;

    [ObservableProperty]
    private int _reminderMaxPerDay;

    [ObservableProperty]
    private int _quietFromHour;

    [ObservableProperty]
    private int _quietToHour;

    [ObservableProperty]
    private string _spreadsheetUrl = "";

    [ObservableProperty]
    private int _questionCount;

    [ObservableProperty]
    private string _theme = "Dark";

    [ObservableProperty]
    private double _backgroundOpacityPercent;

    [ObservableProperty]
    private bool _soundEnabled;

    [ObservableProperty]
    private bool _shakeOnWrong;

    [ObservableProperty]
    private bool _backgroundTile;

    [ObservableProperty]
    private double _backgroundTileScalePercent = 100;

    [ObservableProperty]
    private string _backgroundOverlayColor = "#000000";

    [ObservableProperty]
    private string _localSourcesSummary = "";

    public SettingsViewModel(
        ISettingsService settings, SyncCoordinator sync, AppBackgroundService background, ILocalSourceService localSources,
        ReminderService reminders, AppShell shell)
    {
        _reminders = reminders;
        _shell = shell;
        _localSources = localSources;
        LocalSources = new ObservableCollection<LocalSource>(localSources.Sources);
        LocalSourcesSummary = localSources.BuildSummary();
        _settings = settings;
        Sync = sync;
        Background = background;
        BackgroundOpacityPercent = Math.Round(settings.Current.BackgroundOpacity * 100);
        BackgroundOverlayColor = settings.Current.BackgroundOverlayColor;
        BackgroundTile = settings.Current.BackgroundTile;
        SoundEnabled = settings.Current.SoundEnabled;
        ShakeOnWrong = settings.Current.ShakeOnWrong;
        BackgroundTileScalePercent = Math.Round(settings.Current.BackgroundTileScale * 100);

        StartWithWindows = ReadAutoStart();
        RemindersEnabled = settings.Current.RemindersEnabled;
        ReminderIntervalMinutes = settings.Current.ReminderIntervalMinutes;
        ReminderMaxPerDay = settings.Current.ReminderMaxPerDay;
        QuietFromHour = settings.Current.ReminderQuietFromHour;
        QuietToHour = settings.Current.ReminderQuietToHour;

        SpreadsheetUrl = settings.Current.SpreadsheetUrl ?? "";
        QuestionCount = settings.Current.QuizQuestionCount;
        Theme = settings.Current.Theme;
        _loading = false;
    }

    public override string Title => "Настройки";

    public override NavSection Section => NavSection.Settings;

    public SyncCoordinator Sync { get; }

    public AppBackgroundService Background { get; }

    public ObservableCollection<LocalSource> LocalSources { get; }

    [RelayCommand]
    private async Task AddSourceAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Файл с предложениями (одна строка — одно предложение)",
            Filter = "Текстовые файлы (*.txt)|*.txt",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var path in dialog.FileNames)
        {
            try
            {
                LocalSources.Add(await _localSources.AddAsync(path));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось добавить файл {Path.GetFileName(path)}:\n{ex.Message}", "EngMemAssist",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        LocalSourcesSummary = _localSources.BuildSummary();
    }

    [RelayCommand]
    private void RemoveSource(LocalSource? source)
    {
        if (source is null)
        {
            return;
        }

        _localSources.Remove(source.Id);
        LocalSources.Remove(source);
        LocalSourcesSummary = _localSources.BuildSummary();
    }

    [RelayCommand]
    private Task SupplementFromSourcesAsync() => Sync.SupplementAsync();

    public ColorPreset[] OverlayColors => AppBackgroundService.Presets;

    partial void OnBackgroundOpacityPercentChanged(double value)
    {
        if (!_loading)
        {
            Background.SetOpacity(value / 100.0);
        }
    }

    partial void OnBackgroundTileScalePercentChanged(double value)
    {
        if (!_loading)
        {
            Background.SetTileScale(value / 100.0);
        }
    }

    public int[] ReminderIntervalOptions { get; } = [15, 30, 45, 60, 90, 120, 180, 240];

    public int[] ReminderMaxPerDayOptions { get; } = [1, 2, 3, 5, 8, 10, 15];

    public int[] HourOptions { get; } = Enumerable.Range(0, 24).ToArray();

    private static bool ReadAutoStart()
    {
        try
        {
            return AutoStart.IsEnabled;
        }
        catch (Exception)
        {
            return false;
        }
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        try
        {
            AutoStart.Set(value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось изменить автозапуск:\n{ex.Message}", "EngMemAssist",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    partial void OnRemindersEnabledChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        _settings.Current.RemindersEnabled = value;
        _settings.Save();
        _shell.RefreshTray();
    }

    partial void OnReminderIntervalMinutesChanged(int value) =>
        SaveReminder(s => s.ReminderIntervalMinutes = value);

    partial void OnReminderMaxPerDayChanged(int value) =>
        SaveReminder(s => s.ReminderMaxPerDay = value);

    partial void OnQuietFromHourChanged(int value) =>
        SaveReminder(s => s.ReminderQuietFromHour = value);

    partial void OnQuietToHourChanged(int value) =>
        SaveReminder(s => s.ReminderQuietToHour = value);

    private void SaveReminder(Action<AppSettings> apply)
    {
        if (_loading)
        {
            return;
        }

        apply(_settings.Current);
        _settings.Save();
    }

    [RelayCommand]
    private Task TestReminderAsync() => _reminders.ShowTestAsync();

    partial void OnSoundEnabledChanged(bool value)
    {
        if (!_loading)
        {
            _settings.Current.SoundEnabled = value;
            _settings.Save();
        }
    }

    partial void OnShakeOnWrongChanged(bool value)
    {
        if (!_loading)
        {
            _settings.Current.ShakeOnWrong = value;
            _settings.Save();
        }
    }

    partial void OnBackgroundTileChanged(bool value)
    {
        if (!_loading)
        {
            Background.SetTile(value);
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
