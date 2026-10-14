using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;

namespace EngMemAssist.ViewModels;

/// <summary>Окно подключения таблицы: ссылка проверяется реальным чтением таблицы и сохраняется только при успехе.</summary>
public sealed partial class ConnectSpreadsheetViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IWordSource _source;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string _url = "";

    [ObservableProperty]
    private string _error = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private bool _isBusy;

    public ConnectSpreadsheetViewModel(ISettingsService settings, IWordSource source)
    {
        _settings = settings;
        _source = source;
        Url = settings.Current.SpreadsheetUrl ?? "";
    }

    public string ClientSecretPath => AppPaths.ClientSecretFile;

    public bool ClientSecretMissing => !File.Exists(AppPaths.ClientSecretFile);

    /// <summary>true — подключено, false — отмена.</summary>
    public event Action<bool>? CloseRequested;

    private bool CanConnect() => !IsBusy && !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        Error = "";

        if (!SpreadsheetUrl.TryParseId(Url, out _))
        {
            Error = "Не удалось распознать ссылку. Скопируйте адрес таблицы из браузера (docs.google.com/spreadsheets/d/…).";
            return;
        }

        var previous = _settings.Current.SpreadsheetUrl;
        IsBusy = true;
        try
        {
            _settings.Current.SpreadsheetUrl = Url.Trim();

            // Чтение таблицы заодно запускает вход в Google (откроется браузер) и проверяет доступ.
            var rows = await _source.LoadAsync();
            if (rows.Count == 0)
            {
                _settings.Current.SpreadsheetUrl = previous;
                Error = "Таблица доступна, но в ней не найдено ни одной пары «слово — перевод» (колонки A и B, затем D и E…).";
                return;
            }

            _settings.Save();
            CloseRequested?.Invoke(true);
        }
        catch (SpreadsheetAccessException ex)
        {
            _settings.Current.SpreadsheetUrl = previous;
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            _settings.Current.SpreadsheetUrl = previous;
            Error = $"Непредвиденная ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
