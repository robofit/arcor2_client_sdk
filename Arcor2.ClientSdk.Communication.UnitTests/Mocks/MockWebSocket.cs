using Arcor2.ClientSdk.Communication.Design;

namespace Arcor2.ClientSdk.Communication.UnitTests.Mocks;

internal class MockWebSocket : IWebSocket {
    public WebSocketState State { get; set; }
    public event EventHandler<WebSocketMessageEventArgs>? OnMessage;
    public event EventHandler<WebSocketCloseEventArgs>? OnClose;
    public event EventHandler<WebSocketErrorEventArgs>? OnError;
    public event EventHandler? OnOpen;
    public Task ConnectAsync(Uri url) {
        throw new NotImplementedException();
    }

    public Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
        string? statusDescription = null) {
        throw new NotImplementedException();
    }

    public Task SendAsync(IEnumerable<byte> bytes) {
        throw new NotImplementedException();
    }

    public Task SendAsync(byte[] bytes) {
        throw new NotImplementedException();
    }

    public Task SendAsync(string text) {
        throw new NotImplementedException();
    }

    public void AddExpectedRequest(string text) {

    }

    public void AddExpectedResponse(string text) {

    }
}