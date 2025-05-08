using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Pages;
public partial class SettingsPage : INavigableView<SettingsViewModel> {
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel) {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
