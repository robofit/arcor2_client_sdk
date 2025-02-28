using System;

namespace Arcor2.ClientSdk.Communication.Design {
    /// <summary>
    /// Event arguments for WebSocket error events.
    /// </summary>
    public class WebSocketErrorEventArgs : EventArgs {
        public Exception Exception { get; set; }

        public WebSocketErrorEventArgs(Exception exception) {
            Exception = exception;
        }
    }
}