﻿using System;

namespace Arcor2.ClientSdk.Communication.Design {
    /// <summary>
    /// Event arguments for WebSocket message events.
    /// </summary>
    public class WebSocketMessageEventArgs : EventArgs {
        public byte[] Data { get; set; }
        public WebSocketMessageType MessageType { get; set; }

        public WebSocketMessageEventArgs(byte[] data, WebSocketMessageType messageType) {
            Data = data;
            MessageType = messageType;
        }
    }
}