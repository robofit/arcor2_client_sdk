using System;

namespace Arcor2.ClientSdk.Communication.Design {
    /// <summary>
    ///     Event arguments for WebSocket error events.
    /// </summary>
    public class WebSocketErrorEventArgs : EventArgs {
        public WebSocketErrorEventArgs(Exception exception) {
            Exception = exception;
        }

        public Exception Exception { get; set; }
    }
}