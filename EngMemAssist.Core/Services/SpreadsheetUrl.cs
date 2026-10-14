using System.Text.RegularExpressions;

namespace EngMemAssist.Core.Services;

/// <summary>Разбор ссылки на Google Spreadsheet.</summary>
public static class SpreadsheetUrl
{
    private static readonly Regex UrlRegex = new(@"/spreadsheets/d/([A-Za-z0-9\-_]+)", RegexOptions.Compiled);
    private static readonly Regex RawIdRegex = new(@"^[A-Za-z0-9\-_]{20,}$", RegexOptions.Compiled);

    /// <summary>Достаёт ID таблицы из ссылки. Также принимает сам ID.</summary>
    public static bool TryParseId(string? input, out string id)
    {
        id = "";
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();

        var match = UrlRegex.Match(text);
        if (match.Success)
        {
            id = match.Groups[1].Value;
            return true;
        }

        if (RawIdRegex.IsMatch(text))
        {
            id = text;
            return true;
        }

        return false;
    }
}
