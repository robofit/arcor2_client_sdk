using Arcor2.ClientSdk.Communication.UnitTests.Mocks;

namespace Arcor2.ClientSdk.Communication.UnitTests;

public class TestBase : IDisposable {
    // Too low values (<10) cause even valid tests to fail.
    private const int Timeout = 100;
    protected readonly Uri ValidUri = new("wss://www.random_ws_uri.com:80");

    protected Arcor2Client Client = new(new MockWebSocket(), new Arcor2ClientSettings { RpcTimeout = Timeout });

    protected bool ConnectionClosedEventRaised;

    protected bool ConnectionOpenedEventRaised;

    public TestBase() {
        // Subscribe to events in the constructor
        Client.ConnectionOpened += (_, _) => ConnectionOpenedEventRaised = true;
        Client.ConnectionClosed += (_, _) => ConnectionClosedEventRaised = true;
    }

    protected MockWebSocket WebSocket => (Client.GetUnderlyingWebSocket() as MockWebSocket)!;

    public void Dispose() =>
        // Recreate the client
        Client = new Arcor2Client(new MockWebSocket(), new Arcor2ClientSettings { RpcTimeout = Timeout });
}