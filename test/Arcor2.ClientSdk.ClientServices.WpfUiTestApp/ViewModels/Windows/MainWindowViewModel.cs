using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Views.Pages;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Windows;
public partial class MainWindowViewModel : ObservableObject {
    [ObservableProperty]
    private string _applicationTitle = "ARCOR2 Scene Browser - Example Application";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems = new()
    {
        new NavigationViewItem()
        {
            Content = "Connect",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
            TargetPageType = typeof(ConnectionPage)
        },
        new NavigationViewItem()
        {
            Content = "Scenes",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Cube24 },
            TargetPageType = typeof(ScenesPage)
        }
    };

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = new()
    {
        new NavigationViewItem()
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(SettingsPage)
        }
    };

    [ObservableProperty]
    private ObservableCollection<MenuItem> _trayMenuItems = new()
    {
    };
}
