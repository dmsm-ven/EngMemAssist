using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EngMemAssist.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Колесо мыши прокручивает страницу, даже если курсор над раскрытым expander, ползунком или карточкой.
    /// Открытые выпадающие списки прокручиваются сами.
    /// </summary>
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || IsInsideOpenDropDown(e.OriginalSource as DependencyObject))
        {
            return;
        }

        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static bool IsInsideOpenDropDown(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ComboBox { IsDropDownOpen: true })
            {
                return true;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return false;
    }
}
