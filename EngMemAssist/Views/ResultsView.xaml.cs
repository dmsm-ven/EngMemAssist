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
        if (DataContext is ResultsViewModel { ShowConfetti: true })
        {
            ConfettiHelper.Launch(ConfettiCanvas);
        }
    }
}
