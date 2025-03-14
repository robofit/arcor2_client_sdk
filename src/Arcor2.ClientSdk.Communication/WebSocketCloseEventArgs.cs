using System;

namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    ///     Event arguments for WebSocket close event.
    /// </summary>
    public class WebSocketCloseEventArgs : EventArgs {
        /// <summary>
        ///     The WebSocket close status code. Empty if unknown.
        /// </summary>
        public WebSocketCloseStatus? CloseStatus { get; set; }

        public string? CloseStatusDescription { get; set; }
    }
}