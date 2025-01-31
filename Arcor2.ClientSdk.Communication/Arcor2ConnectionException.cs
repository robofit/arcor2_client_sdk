using System;

namespace Arcor2.ClientSdk.Communication {
    public class WebSocketConnectionException : Exception {
        public WebSocketConnectionException() : base() { }
        public WebSocketConnectionException(string message) : base(message) { }
        public WebSocketConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
