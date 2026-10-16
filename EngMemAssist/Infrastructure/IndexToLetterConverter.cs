using System.Globalization;
using System.Windows.Data;

namespace EngMemAssist.Infrastructure;

/// <summary>0 → «A», 1 → «B», … (для подписей вариантов ответа).</summary>
public sealed class IndexToLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int index and >= 0 ? ((char)('A' + index % 26)).ToString() : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
