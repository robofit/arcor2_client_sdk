using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.ViewModels.Pages;

public partial class ConnectionViewModel(Arcor2Service arcor2Service, ISnackbarService snackbarService) : ObservableObject {
    [ObservableProperty]
    private string _domain = "localhost";

    [ObservableProperty]
    private int _port = 6789;

    [ObservableProperty]
    private string _username = $"user_{Guid.NewGuid().ToString()[..3]}";

    [ObservableProperty] 
    private string _connectionStatus = arcor2Service.Session?.ConnectionState.ToString() ?? "None";

    public string CurrentUsername => arcor2Service.Session?.Username ?? "-";

    [RelayCommand]
    private async Task Connect() {
        try {
            // Recreate the object
            if(arcor2Service.Session != null && 
               arcor2Service.Session.ConnectionState != Arcor2SessionState.Closed && 
               arcor2Service.Session.ConnectionState != Arcor2SessionState.None) {
                await arcor2Service.Session!.CloseAsync();
            }
            arcor2Service.CreateNewSession();

            // Register important handlers
            arcor2Service.Session!.ConnectionOpened += (_,_) => { ConnectionStatus = "Open"; };
            arcor2Service.Session.ConnectionClosed += (_,_) => { ConnectionStatus = "Closed"; };
            arcor2Service.Session.ConnectionError += (_, args) => { snackbarService.Show("Error", args.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(8)); };

            // Init
            await arcor2Service.Session.ConnectAsync(Domain, (ushort) Port);
            await arcor2Service.Session.InitializeAsync();
            await arcor2Service.Session.RegisterAndSubscribeAsync(Username);

            // Raise changes
            OnPropertyChanged(nameof(CurrentUsername));

            snackbarService.Show("Success", $"Successfully connected as {CurrentUsername}", ControlAppearance.Success, null, TimeSpan.FromSeconds(8));
        }
        catch(Exception ex) {
            ConnectionStatus = "Closed";
            snackbarService.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(8));
        }
    }
}