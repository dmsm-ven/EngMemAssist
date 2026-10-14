using EngMemAssist.ViewModels;
using Wpf.Ui.Controls;

namespace EngMemAssist.Views;

public partial class ConnectSpreadsheetWindow : FluentWindow
{
    public ConnectSpreadsheetWindow(ConnectSpreadsheetViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += connected => DialogResult = connected;
    }
}
