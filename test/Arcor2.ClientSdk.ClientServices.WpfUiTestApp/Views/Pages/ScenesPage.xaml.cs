using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Pages;
public partial class ScenesPage : INavigableView<ScenesViewModel> {
    public ScenesViewModel ViewModel { get; }

    public ScenesPage(ScenesViewModel viewModel) {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
