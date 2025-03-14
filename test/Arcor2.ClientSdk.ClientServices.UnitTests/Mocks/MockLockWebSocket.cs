#pragma warning disable CS0067 // Event is never used
#pragma warning disable CS1998 // Async method lacks 'await' operator and will run synchronously

using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.Design;

namespace Arcor2.ClientSdk.ClientServices.UnitTests.Mocks;

public class MockLockWebSocket : IWebSocket {
    public uint WriteLockCalled;
    public uint WriteUnlockCalled;
    public WebSocketState State { get; private set; } = WebSocketState.None;
    public event EventHandler<WebSocketMessageEventArgs>? OnMessage {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event EventHandler<WebSocketCloseEventArgs>? OnClose {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event EventHandler<WebSocketErrorEventArgs>? OnError {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    public event EventHandler? OnOpen {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public async Task ConnectAsync(Uri url) => State = WebSocketState.Open;

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
        string? statusDescription = null) =>
        State = WebSocketState.Closed;

    public async Task SendAsync(string text) {
        if(text.Contains("WriteLock")) {
            WriteLockCalled++;
        }

        if(text.Contains("WriteUnlock")) {
            WriteUnlockCalled++;
        }
    }
}