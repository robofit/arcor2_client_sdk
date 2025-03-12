using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.Design;

namespace Arcor2.ClientSdk.ClientServices.UnitTests.Mocks;

public class MockLockWebSocket : IWebSocket
{
    public WebSocketState State { get; private set; } = WebSocketState.None;
    public event EventHandler<WebSocketMessageEventArgs>? OnMessage;
    public event EventHandler<WebSocketCloseEventArgs>? OnClose;
    public event EventHandler<WebSocketErrorEventArgs>? OnError;
    public event EventHandler? OnOpen;

    public uint WriteLockCalled = 0;
    public uint WriteUnlockCalled = 0;

    public async Task ConnectAsync(Uri url) {
        State = WebSocketState.Open;
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
        string? statusDescription = null) {
        State = WebSocketState.Closed;
    }

    public async Task SendAsync(string text) {
        if (text.Contains("WriteLock")) {
            WriteLockCalled++;
        }

        if (text.Contains("WriteUnlock")) {
            WriteUnlockCalled++;
        }
    }
}