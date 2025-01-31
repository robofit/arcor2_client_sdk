using System;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.Communication.Design
{
    /// <summary>
    /// A unified adapter for different WebSocket implementation provider. 
    /// </summary>
    public interface IWebSocket {
        /// <summary>
        /// The current state of underlying WebSocket. None by default.
        /// </summary>
        WebSocketState State { get; }

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        /// <remarks>
        /// The event is resistant to exceptions in event handlers and will not fail.
        /// </remarks>
        event EventHandler<WebSocketMessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the connection is closed. 
        /// </summary>
        event EventHandler<WebSocketCloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when connection error happens in message receive loop.
        /// </summary>
        /// <remarks>The reason we use an event here is due to Unity's threading support. </remarks>
        event EventHandler<WebSocketErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the connection is established successfully.
        /// </summary>
        event EventHandler OnOpen;

        /// <summary>
        /// Initiates a connection and starts listening for messages.
        /// </summary>
        /// <param name="url">The WebSocket server.</param>
        /// <exception cref="InvalidOperationException">When invoked in state other than <see cref="WebSocketState.None"/>.</exception>
        /// <exception cref="WebSocketConnectionException">When the WebSocket fails to connect.</exception>
        Task ConnectAsync(Uri url);
        /// <summary>
        /// Closes the connection.
        /// </summary>
        Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? statusDescription = null);
        /// <summary>
        /// Sends a string to the WebSocket connection. Supports multiple sends at the same time.
        /// </summary>
        /// <exception cref="InvalidOperationException">When invoked in state other than <see cref="WebSocketState.Open"/>.</exception>
        Task SendAsync(string text);
    }
}