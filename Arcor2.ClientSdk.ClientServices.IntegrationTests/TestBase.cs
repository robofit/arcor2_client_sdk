using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests;

public class TestBase : IAsyncLifetime {
    private Arcor2ServerFixture server = null!;
    protected Arcor2Session Session { get; private set; } = null!;
    protected Uri Uri => server.Uri;
    protected string Host => server.Host;
    protected ushort Port => server.Port;

    public async Task InitializeAsync() {
        server = new Arcor2ServerFixture();
        await server.InitializeAsync();

        Session = new Arcor2Session();
    }

    public async Task DisposeAsync() {
        if(Session != null!) {
            Session.Dispose();
        }
        if(server != null!) {
            await server.DisposeAsync();
        }
    }
}