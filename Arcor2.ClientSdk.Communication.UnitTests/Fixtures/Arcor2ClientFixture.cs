using Arcor2.ClientSdk.Communication.UnitTests.Mocks;

namespace Arcor2.ClientSdk.Communication.UnitTests.Fixtures;

public class Arcor2ClientFixture : IDisposable
{
    protected Arcor2Client<MockWebSocket> Client = new(new Arcor2ClientSettings {
        RpcTimeout = Timeout
    });

    protected MockWebSocket WebSocket => Client.GetUnderlyingWebSocket();
    protected readonly Uri ValidUri = new($"wss://www.random_ws_uri.com:80");

    protected bool ConnectionOpenedEventRaised;
    protected bool ConnectionClosedEventRaised;

    // Too low values (<10) cause even valid tests to fail.
    private const int Timeout = 100;

    public Arcor2ClientFixture()
    {
        // Subscribe to events in the constructor
        Client.OnConnectionOpened += (_, _) => ConnectionOpenedEventRaised = true;
        Client.OnConnectionClosed += (_, _) => ConnectionClosedEventRaised = true;
    }

    public void Dispose()
    {
        // Recreate the client
        Client = new Arcor2Client<MockWebSocket>(new Arcor2ClientSettings {
            RpcTimeout = Timeout
        });
    }
}