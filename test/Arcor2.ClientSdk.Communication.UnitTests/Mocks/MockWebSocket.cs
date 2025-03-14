#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable 67 // Event is never used

using Arcor2.ClientSdk.Communication.Design;
using System.Text;
using WebSocketState = Arcor2.ClientSdk.Communication.Design.WebSocketState;

namespace Arcor2.ClientSdk.Communication.UnitTests.Mocks;

public class MockWebSocket : IWebSocket {
    public readonly Queue<string> ReceivedMessages = new();

    public readonly Queue<string> SentMessages = new();

    public bool TestingShouldFailConnect { get; set; } = false;
    public WebSocketState State { get; set; }
    public event EventHandler<WebSocketMessageEventArgs>? OnMessage;
    public event EventHandler<WebSocketCloseEventArgs>? OnClose;
    public event EventHandler<WebSocketErrorEventArgs>? OnError;
    public event EventHandler? OnOpen;

    public async Task ConnectAsync(Uri url) {
        if(State != WebSocketState.None) {
            throw new InvalidOperationException("ConnectAsync method can only be invoked once.");
        }

        if(!TestingShouldFailConnect) {
            State = WebSocketState.Open;
            OnOpen?.Invoke(this, EventArgs.Empty);
        }
        else {
            State = WebSocketState.Closed;
            throw new WebSocketConnectionException("Connection failed.");
        }
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
        string? statusDescription = null) {
        State = WebSocketState.Closed;
        OnClose?.Invoke(this,
            new WebSocketCloseEventArgs { CloseStatus = closeStatus, CloseStatusDescription = statusDescription });
    }

    public async Task SendAsync(string text) => SentMessages.Enqueue(text);

    public async Task SendAsync(IEnumerable<byte> bytes) => throw new NotImplementedException();

    public async Task SendAsync(byte[] bytes) => throw new NotImplementedException();

    public void ReceiveMockMessage(string message) {
        ReceivedMessages.Enqueue(message);
        OnMessage?.Invoke(this,
            new WebSocketMessageEventArgs(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text));
    }
}