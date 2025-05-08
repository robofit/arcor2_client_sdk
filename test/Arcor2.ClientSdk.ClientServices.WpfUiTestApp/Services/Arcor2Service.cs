using System.Windows.Threading;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Services;
/// <summary>
/// Simple service wrapper for providing sessions.
/// </summary>
public class Arcor2Service {
    public Arcor2Session? Session { get; set; }

    public void CreateNewSession() { 
        Session = new Arcor2Session(new Arcor2SessionSettings {
            SynchronizationAction = @continue => {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    @continue);
            }
        });
    }
}
