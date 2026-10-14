using EngMemAssist.Core.Abstractions;
using EngMemAssist.Core.Exceptions;
using EngMemAssist.Core.Services;
using EngMemAssist.Core.Settings;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace EngMemAssist.Integrations;

/// <summary>
/// Читает слова из Google Spreadsheet (OAuth для Desktop-приложения).
/// Макет таблицы: [ENG][RU][разделитель] повторяется блоками по три колонки: A,B | D,E | G,H ...
/// Заголовков нет.
/// </summary>
public sealed class GoogleSheetsWordSource : IWordSource
{
    private const int BlockWidth = 3;

    private readonly ISettingsService _settings;

    public GoogleSheetsWordSource(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<SourceWord>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!SpreadsheetUrl.TryParseId(_settings.Current.SpreadsheetUrl, out var spreadsheetId))
        {
            throw new SpreadsheetAccessException("Ссылка на таблицу не задана или имеет неверный формат.");
        }

        if (!File.Exists(AppPaths.ClientSecretFile))
        {
            throw new SpreadsheetAccessException(
                $"Не найден файл client_secret.json. Скачайте его из Google Cloud Console (OAuth client, тип Desktop app) и положите сюда:\n{AppPaths.ClientSecretFile}");
        }

        try
        {
            var credential = await AuthorizeAsync(cancellationToken);

            using var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "EngMemAssist",
            });

            var meta = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(cancellationToken);
            var sheetTitle = meta.Sheets?.FirstOrDefault()?.Properties?.Title;
            if (string.IsNullOrEmpty(sheetTitle))
            {
                throw new SpreadsheetAccessException("В таблице нет ни одного листа.");
            }

            var range = $"'{sheetTitle.Replace("'", "''")}'";
            var response = await service.Spreadsheets.Values.Get(spreadsheetId, range).ExecuteAsync(cancellationToken);

            return ParseRows(response.Values);
        }
        catch (SpreadsheetAccessException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (GoogleApiException ex)
        {
            throw new SpreadsheetAccessException(DescribeGoogleError(ex), ex);
        }
        catch (HttpRequestException ex)
        {
            throw new SpreadsheetAccessException("Нет соединения с Google. Проверьте интернет.", ex);
        }
        catch (Exception ex)
        {
            throw new SpreadsheetAccessException($"Ошибка доступа к таблице: {ex.Message}", ex);
        }
    }

    private static async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(AppPaths.ClientSecretFile);
        var secrets = (await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken)).Secrets;

        // При первом запуске откроется браузер для входа; токен сохраняется в AppData и дальше обновляется сам.
        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            [SheetsService.Scope.SpreadsheetsReadonly],
            "user",
            cancellationToken,
            new FileDataStore(AppPaths.GoogleTokenDirectory, fullPath: true));
    }

    /// <summary>Разбирает значения листа в пары слово-перевод. Пустые строки и неполные пары пропускаются.</summary>
    public static IReadOnlyList<SourceWord> ParseRows(IList<IList<object>>? rows)
    {
        var result = new List<SourceWord>();
        if (rows is null)
        {
            return result;
        }

        foreach (var row in rows)
        {
            for (var column = 0; column < row.Count; column += BlockWidth)
            {
                var english = Cell(row, column);
                var translation = Cell(row, column + 1);
                if (english.Length > 0 && translation.Length > 0)
                {
                    result.Add(new SourceWord(english, translation));
                }
            }
        }

        return result;
    }

    private static string Cell(IList<object> row, int index) =>
        index < row.Count ? row[index]?.ToString()?.Trim() ?? "" : "";

    private static string DescribeGoogleError(GoogleApiException ex) => (int)ex.HttpStatusCode switch
    {
        403 => "Нет доступа к таблице. Откройте к ней доступ для вашего Google-аккаунта и убедитесь, что Google Sheets API включён в проекте.",
        404 => "Таблица не найдена. Проверьте ссылку.",
        429 => "Google ограничил число запросов. Повторите позже.",
        _ => $"Ошибка Google API ({(int)ex.HttpStatusCode}): {ex.Message}",
    };
}
