using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;

namespace EngMemAssist.Infrastructure;

/// <summary>
/// Запускает синхронизацию слов с таблицей (при старте и по кнопке), показывает ход работы
/// и ошибки: таблица недоступна — Warning, Too Many Requests от сервиса предложений — Information.
/// </summary>
public sealed partial class SyncCoordinator : ObservableObject
{
    private const string AppTitle = "EngMemAssist";

    private readonly IWordSyncService _sync;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly ILemmatizerDiagnostics _lemmatizerDiagnostics;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "Готово к работе";

    public SyncCoordinator(
        IWordSyncService sync,
        ISettingsService settings,
        IDialogService dialogs,
        ILemmatizerDiagnostics lemmatizerDiagnostics)
    {
        _sync = sync;
        _settings = settings;
        _dialogs = dialogs;
        _lemmatizerDiagnostics = lemmatizerDiagnostics;
    }

    /// <summary>Синхронизация завершилась (успешно или с ошибкой): страницы могут перечитать данные.</summary>
    public event EventHandler? Completed;

    public bool IsSpreadsheetConfigured => SpreadsheetUrl.TryParseId(_settings.Current.SpreadsheetUrl, out _);

    public bool IsNotConfigured => !IsSpreadsheetConfigured;

    /// <summary>Запуск при старте: если таблица не подключена — окно подключения, затем синхронизация.</summary>
    public async Task RunStartupAsync()
    {
        if (!IsSpreadsheetConfigured)
        {
            var connected = _dialogs.ShowConnectSpreadsheet() == true;
            RefreshConfigured();

            if (!connected)
            {
                Status = "Таблица не подключена. Нажмите «Подключить таблицу».";
                Completed?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        await SyncAsync();
    }

    /// <summary>Окно подключения (смена таблицы) и, при успехе, синхронизация.</summary>
    public async Task ConnectAndSyncAsync()
    {
        var connected = _dialogs.ShowConnectSpreadsheet() == true;
        RefreshConfigured();

        if (connected)
        {
            await SyncAsync();
        }
    }

    public async Task SyncAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!IsSpreadsheetConfigured)
        {
            await ConnectAndSyncAsync();
            return;
        }

        IsBusy = true;
        try
        {
            var progress = new Progress<string>(message => Status = message);
            var result = await _sync.SyncAsync(progress);

            Status = BuildStatus(result);

            if (result.RateLimited)
            {
                MessageBox.Show(
                    $"{result.RateLimitMessage}\n\nПредложения для оставшихся слов будут загружены при следующей синхронизации.",
                    AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (SpreadsheetAccessException ex)
        {
            Status = "Не удалось подключиться к таблице";
            MessageBox.Show(
                $"Не удалось подключиться к Google Spreadsheet.\n\n{ex.Message}",
                AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (OperationCanceledException)
        {
            Status = "Синхронизация отменена";
        }
        catch (Exception ex)
        {
            Status = "Ошибка синхронизации";
            MessageBox.Show($"Ошибка синхронизации:\n{ex.Message}", AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            RefreshConfigured();
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }

    private string BuildStatus(SyncResult result)
    {
        var parts = new List<string> { $"Слов в словаре: {result.TotalActive}" };

        if (result.Added > 0)
        {
            parts.Add($"добавлено {result.Added}");
        }

        if (result.Updated > 0)
        {
            parts.Add($"обновлено {result.Updated}");
        }

        if (result.Removed > 0)
        {
            parts.Add($"убрано {result.Removed}");
        }

        if (result.SentencesAdded > 0)
        {
            parts.Add($"предложений загружено {result.SentencesAdded}");
        }

        if (result.FailedWords > 0)
        {
            parts.Add($"не удалось загрузить предложения для {result.FailedWords} сл.");
        }

        if (result.WordsWithoutSentences > 0)
        {
            parts.Add($"без предложений: {result.WordsWithoutSentences} сл. (в квизе не участвуют)");
        }

        var text = string.Join(", ", parts) + ".";

        if (_lemmatizerDiagnostics.DegradedReason is { } reason)
        {
            text += $"\nВнимание: лемматизатор работает в упрощённом режиме ({reason}).";
        }

        return text;
    }

    private void RefreshConfigured()
    {
        OnPropertyChanged(nameof(IsSpreadsheetConfigured));
        OnPropertyChanged(nameof(IsNotConfigured));
    }
}
