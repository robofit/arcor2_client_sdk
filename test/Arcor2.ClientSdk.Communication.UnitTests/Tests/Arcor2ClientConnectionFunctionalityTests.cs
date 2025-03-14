using Arcor2.ClientSdk.Communication.Design;

namespace Arcor2.ClientSdk.Communication.UnitTests.Tests;

public class Arcor2ClientConnectionFunctionalityTests : TestBase {
    [Fact]
    public void ConnectAsync_DefaultState_None() {
        Assert.Equal(WebSocketState.None, Client.ConnectionState);
        Assert.False(ConnectionOpenedEventRaised);
        Assert.False(ConnectionClosedEventRaised);
    }

    [Fact]
    public async Task ConnectAsync_NonWSUri_Throws() {
        var invalidUri = new Uri("https://www.google.com");

        await Assert.ThrowsAsync<UriFormatException>(async () => await Client.ConnectAsync(invalidUri));
        Assert.False(ConnectionOpenedEventRaised);
    }

    [Fact]
    public async Task ConnectAsync_InvalidDomain_Throws() {
        var invalidDomain = "this is :// :// \\\\invalid dom....ain";

        await Assert.ThrowsAsync<UriFormatException>(async () => await Client.ConnectAsync(invalidDomain, 0));
        Assert.False(ConnectionOpenedEventRaised);
    }

    [Fact]
    public async Task ConnectAsync_ConnectionError_Throws() {
        WebSocket.TestingShouldFailConnect = true;

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await Client.ConnectAsync(ValidUri));
        Assert.False(ConnectionOpenedEventRaised);
    }

    [Fact]
    public async Task ConnectAsync_Connection_Success() {
        var exception = await Record.ExceptionAsync(async () => await Client.ConnectAsync(ValidUri));

        Assert.Null(exception);
        Assert.Equal(WebSocketState.Open, Client.ConnectionState);
        Assert.True(ConnectionOpenedEventRaised);
    }

    [Fact]
    public async Task ConnectAsync_MultipleConnections_Throws() {
        await Client.ConnectAsync(ValidUri);
        ConnectionOpenedEventRaised = false; // Reset for second connection

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Client.ConnectAsync(ValidUri));
        Assert.False(ConnectionOpenedEventRaised);
    }

    [Fact]
    public async Task CloseAsync_NotConnected_Throws() {
        await Assert.ThrowsAsync<InvalidOperationException>(Client.CloseAsync);
        Assert.False(ConnectionClosedEventRaised);
    }

    [Fact]
    public async Task CloseAsync_Connected_Success() {
        await Client.ConnectAsync(ValidUri);

        var exception = await Record.ExceptionAsync(Client.CloseAsync);

        Assert.Null(exception);
        Assert.Equal(WebSocketState.Closed, Client.ConnectionState);
        Assert.True(ConnectionClosedEventRaised);
    }
}