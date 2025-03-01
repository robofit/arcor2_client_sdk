using Arcor2.ClientSdk.ClientServices.Enums;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionInitTests : TestBase {
    [Fact]
    public async Task ConnectionSequence_Valid_Connects() {
        // Setup
        var connectRaised = false;
        var closedRaised = false;
        Session.ConnectionOpened += (_, _) => connectRaised = true;
        Session.ConnectionClosed += (_, args) => closedRaised = true;

        Assert.Equal(Arcor2SessionState.None, Session.ConnectionState);

        await Session.ConnectAsync(Uri);
        Assert.Equal(Arcor2SessionState.Open, Session.ConnectionState);
        Assert.Equal(NavigationState.None, Session.NavigationState);
        Assert.True(connectRaised);

        await Session.CloseAsync();
        Assert.Equal(Arcor2SessionState.Closed, Session.ConnectionState);
        Assert.True(closedRaised);
    }
}
