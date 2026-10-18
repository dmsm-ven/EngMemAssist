using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EngMemAssist.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>Фокус на «Начать квиз»: Enter запускает тест, Tab/стрелки меняют стратегию, не нужна мышь.</summary>
    private void OnLoaded(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (StartButton.IsEnabled)
            {
                StartButton.Focus();
            }
        }, DispatcherPriority.Input);
}
