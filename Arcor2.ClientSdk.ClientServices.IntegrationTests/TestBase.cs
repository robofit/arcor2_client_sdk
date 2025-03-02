using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests;

public class TestBase(Arcor2ServerFixture serverFixture, ITestOutputHelper output) : IAsyncLifetime, IClassFixture<Arcor2ServerFixture> {
    protected Arcor2Session Session { get; private set; } = null!;
    protected IArcor2Logger Logger { get; private set; }
    protected Uri Uri => serverFixture.Uri;
    protected string Host => serverFixture.Host;
    protected ushort Port => serverFixture.Port;

    protected string RandomName() => Guid.NewGuid().ToString();
    public async Task InitializeAsync() {
        Logger = new TestLogger(output);
        Session = new Arcor2Session(logger: Logger);
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
        await Task.CompletedTask;
    }

    // Helpers

    public EventAwaiter<NavigationStateEventArgs> GetNavigationAwaiter() {
        var navigationState = new EventAwaiter<NavigationStateEventArgs>();
        Session.NavigationStateChanged += navigationState.EventHandler;
        return navigationState;
    }
}