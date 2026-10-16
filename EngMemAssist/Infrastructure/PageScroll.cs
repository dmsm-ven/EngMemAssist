using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EngMemAssist.Infrastructure;

/// <summary>
/// Присоединяемое свойство для ScrollViewer страницы: колесо мыши прокручивает страницу,
/// даже если курсор над раскрытым expander, ползунком или карточкой.
/// Не вмешивается, когда курсор над открытым списком ComboBox или над таблицей, у которой есть своя прокрутка.
/// </summary>
public static class PageScroll
{
    public static readonly DependencyProperty ForwardWheelProperty =
        DependencyProperty.RegisterAttached("ForwardWheel", typeof(bool), typeof(PageScroll),
            new PropertyMetadata(false, OnForwardWheelChanged));

    public static bool GetForwardWheel(DependencyObject d) => (bool)d.GetValue(ForwardWheelProperty);

    public static void SetForwardWheel(DependencyObject d, bool value) => d.SetValue(ForwardWheelProperty, value);

    private static void OnForwardWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer)
        {
            return;
        }

        viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        if ((bool)e.NewValue)
        {
            viewer.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not ScrollViewer viewer || ShouldLeaveToChild(e.OriginalSource as DependencyObject))
        {
            return;
        }

        viewer.ScrollToVerticalOffset(viewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static bool ShouldLeaveToChild(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ComboBox { IsDropDownOpen: true })
            {
                return true;
            }

            if (element is DataGrid grid && FindScrollViewer(grid) is { ScrollableHeight: > 0 })
            {
                return true;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return false;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer)
            {
                return viewer;
            }

            if (FindScrollViewer(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
