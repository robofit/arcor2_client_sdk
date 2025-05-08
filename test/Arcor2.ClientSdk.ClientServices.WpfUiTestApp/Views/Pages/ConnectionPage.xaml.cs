using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Pages;
public partial class ConnectionPage : INavigableView<ConnectionViewModel> {
    public ConnectionViewModel ViewModel { get; }

    public ConnectionPage(ConnectionViewModel viewModel) {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
