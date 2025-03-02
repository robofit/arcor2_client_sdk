using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests;

public class TestBase(ITestOutputHelper output) : IAsyncLifetime {
    private Arcor2ServerFixture server;
    protected Arcor2Session Session { get; private set; } = null!;
    protected IArcor2Logger Logger { get; private set; }
    protected Uri Uri => server.Uri;
    protected string Host => server.Host;
    protected ushort Port => server.Port;

    protected string RandomName() => Guid.NewGuid().ToString();
    public async Task InitializeAsync() {
        Logger = new TestLogger(output);
        Session = new Arcor2Session(logger: Logger);
        server = new Arcor2ServerFixture(); 
        await server.InitializeAsync();
        await Task.CompletedTask;
    }

    protected async Task Setup() {
        await Session.ConnectAsync(Uri);
        await Session.InitializeAsync();
        await Session.RegisterAndSubscribeAsync("user" + Guid.NewGuid().ToString()[..4]);
    }

    protected async Task Teardown() {
        await Session.CloseAsync();
    }

    public async Task DisposeAsync() {
        await server.DisposeAsync();
    }

    // Helpers

    public EventAwaiter<NavigationStateEventArgs> GetNavigationAwaiter() {
        var navigationState = new EventAwaiter<NavigationStateEventArgs>();
        Session.NavigationStateChanged += navigationState.EventHandler;
        return navigationState;
    }
}