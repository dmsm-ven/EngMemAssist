using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EngMemAssist.Infrastructure;

/// <summary>
/// Подсветка вариантов ответа после выбора: выбранный — зелёный или красный, а при ошибке верный вариант — зелёный.
/// Значения: вариант, выбранный вариант, был ли выбор верным, верный вариант. Без подсветки возвращает UnsetValue (работает обычный стиль кнопки).
/// </summary>
public sealed class OptionFeedbackBrushConverter : IMultiValueConverter
{
    private static readonly Brush Correct = Freeze(Color.FromArgb(0xD0, 0x2E, 0x7D, 0x32));
    private static readonly Brush Wrong = Freeze(Color.FromArgb(0xD0, 0xC6, 0x28, 0x28));

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4 || values[0] is not string option)
        {
            return DependencyProperty.UnsetValue;
        }

        var chosen = values[1] as string;
        var chosenIsCorrect = values[2] is true;
        var correct = values[3] as string;

        if (chosen is not null && option == chosen)
        {
            return chosenIsCorrect ? Correct : Wrong;
        }

        return correct is not null && option == correct ? Correct : DependencyProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
