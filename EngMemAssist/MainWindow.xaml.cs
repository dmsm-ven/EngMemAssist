using EngMemAssist.ViewModels;
using Wpf.Ui.Controls;

namespace EngMemAssist;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
