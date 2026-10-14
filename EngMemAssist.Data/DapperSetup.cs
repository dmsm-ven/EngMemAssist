using System.Data;
using System.Globalization;
using Dapper;

namespace EngMemAssist.Data;

/// <summary>Настройки Dapper: даты хранятся в SQLite как ISO-8601 текст в UTC.</summary>
internal static class DapperSetup
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        // Для встроенных типов (DateTime) Dapper игнорирует пользовательские обработчики,
        // пока не убрана стандартная привязка типа.
        SqlMapper.RemoveTypeMap(typeof(DateTime));
        SqlMapper.RemoveTypeMap(typeof(DateTime?));

        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
        _initialized = true;
    }

    public static string ToDb(DateTime value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTime Parse(object value) =>
        DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private sealed class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value) =>
            parameter.Value = ToDb(value);

        public override DateTime Parse(object value) => DapperSetup.Parse(value);
    }

    private sealed class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime? value) =>
            parameter.Value = value.HasValue ? ToDb(value.Value) : DBNull.Value;

        public override DateTime? Parse(object value) =>
            value is null or DBNull ? null : DapperSetup.Parse(value);
    }
}
