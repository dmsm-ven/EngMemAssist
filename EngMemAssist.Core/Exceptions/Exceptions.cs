namespace EngMemAssist.Core.Exceptions;

/// <summary>Не удалось получить данные из таблицы-источника (нет файла ключа, нет доступа, нет сети).</summary>
public sealed class SpreadsheetAccessException : Exception
{
    public SpreadsheetAccessException(string message) : base(message)
    {
    }

    public SpreadsheetAccessException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>Сервис предложений вернул ошибку или недоступен.</summary>
public class SentenceProviderException : Exception
{
    public SentenceProviderException(string message) : base(message)
    {
    }

    public SentenceProviderException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>Сервис предложений вернул Too Many Requests (HTTP 429).</summary>
public sealed class TooManyRequestsException : SentenceProviderException
{
    public TooManyRequestsException(string message) : base(message)
    {
    }
}
