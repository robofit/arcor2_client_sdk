using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.Communication.Design
{
    /// <summary>
    /// A unified adapter for different WebSocket implementation provider. 
    /// </summary>
    public interface IWebSocket {
        /// <summary>
        /// The current state of underlying WebSocket.
        /// </summary>
        WebSocketState State { get; }

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<WebSocketMessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the connection is closed by the server. 
        /// </summary>
        event EventHandler<WebSocketCloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when an error happens during any operation.
        /// </summary>
        event EventHandler<WebSocketErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the connection is established successfully.
        /// </summary>
        event EventHandler OnOpen;

        /// <summary>
        /// Initiates a connection.
        /// </summary>
        /// <param name="url">The WebSocket server.</param>
        /// <returns></returns>
        Task ConnectAsync(Uri url);
        /// <summary>
        /// Closes the connection.
        /// </summary>
        Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? statusDescription = null);
        /// <summary>
        /// Sends bytes to the WebSocket connection.
        /// </summary>
        Task SendAsync(IEnumerable<byte> bytes);
        /// <summary>
        /// Sends bytes to the WebSocket connection.
        /// </summary>
        Task SendAsync(byte[] bytes);
        /// <summary>
        /// Sends a string to the WebSocket connection.
        /// </summary>
        Task SendAsync(string text);
    }
}