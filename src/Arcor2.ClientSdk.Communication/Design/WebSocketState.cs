namespace Arcor2.ClientSdk.Communication.Design {
    /// <summary>
    /// State of the WebSocket connection.
    /// </summary>
    public enum WebSocketState {
        /// <summary>
        /// Default state.
        /// </summary>
        None,
        /// <summary>
        /// WebSocket is negotiating a handshake with the server.
        /// </summary>
        /// <remarks> The use of <see cref="Connecting"/> by <see cref="IWebSocket"/> is implementation dependent. Alternate WebSocket implementations may omit this state.</remarks>
        Connecting,
        /// <summary>
        /// WebSocket has negotiated a handshake and is connected.
        /// </summary>
        Open,
        /// <summary>
        /// WebSocket sent or received a close message.
        /// </summary>
        /// <remarks> The use of <see cref="Closing"/> by <see cref="IWebSocket"/> is implementation dependent. Alternate WebSocket implementations may omit this state.</remarks>
        Closing,
        /// <summary>
        /// The connection is closed.
        /// </summary>
        Closed
    }
}