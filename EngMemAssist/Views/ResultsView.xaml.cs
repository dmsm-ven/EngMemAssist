using System.Windows;
using System.Windows.Controls;
using EngMemAssist.Infrastructure;
using EngMemAssist.ViewModels;

namespace EngMemAssist.Views;

public partial class ResultsView : UserControl
{
    public ResultsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Клавиатурный фокус сразу на «Ещё раз»: Enter запускает тест заново, Tab ведёт к остальным кнопкам.
        Dispatcher.BeginInvoke(() => AgainButton.Focus(), System.Windows.Threading.DispatcherPriority.Input);

        if (DataContext is ResultsViewModel { ShowConfetti: true })
        {
            ConfettiHelper.Launch(ConfettiCanvas);
        }
    }
}
