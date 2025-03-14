using System;

namespace Arcor2.ClientSdk.Communication.Design {
    public class WebSocketConnectionException : Exception {
        public WebSocketConnectionException() { }
        public WebSocketConnectionException(string message) : base(message) { }

        public WebSocketConnectionException(string message, Exception innerException) :
            base(message, innerException) { }
    }
}