using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.Communication.Design
{
    public class SystemNetWebSocket : IWebSocket {
        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public WebSocketState State {
            get {
                return webSocket.State switch {
                    System.Net.WebSockets.WebSocketState.None => WebSocketState.None,
                    System.Net.WebSockets.WebSocketState.Connecting => WebSocketState.Connecting,
                    System.Net.WebSockets.WebSocketState.Open => WebSocketState.Open,
                    System.Net.WebSockets.WebSocketState.CloseReceived => WebSocketState.Closing,
                    System.Net.WebSockets.WebSocketState.CloseSent => WebSocketState.Closing,
                    System.Net.WebSockets.WebSocketState.Closed => WebSocketState.Closed,
                    System.Net.WebSockets.WebSocketState.Aborted => WebSocketState.Closed,
                    _ => throw new InvalidOperationException("Invalid state of underlying WebSocket.")
                };
            }
        }

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public event EventHandler<WebSocketMessageEventArgs>? OnMessage;
        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public event EventHandler<WebSocketCloseEventArgs>? OnClose;
        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public event EventHandler<WebSocketErrorEventArgs>? OnError;
        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public event EventHandler? OnOpen;

        private readonly ClientWebSocket webSocket = new ClientWebSocket();
        private Uri? uri;

        private readonly object sendMessageLock = new object();
        private bool sendingMessage;


        // Used to store byte and type pair in send queue
        private class QueueMessage {
            public ArraySegment<byte> Bytes { get; }
            public System.Net.WebSockets.WebSocketMessageType Type { get; }

            public QueueMessage(ArraySegment<byte> bytes, System.Net.WebSockets.WebSocketMessageType type) {
                Bytes = bytes;
                Type = type;
            }
        }

        private Queue<QueueMessage> messageQueue = new Queue<QueueMessage>();

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        public async Task ConnectAsync(Uri uri) {
            if(State != WebSocketState.None) {
                throw new InvalidOperationException("ConnectAsync method can only be invoked once.");
            }

            this.uri = uri;

            try {
                await webSocket.ConnectAsync(uri, CancellationToken.None);
                OnOpen?.Invoke(this, EventArgs.Empty);
                Task.Run(ReceiveAsync);
            }
            catch (Exception ex) {
                OnError?.Invoke(this, new WebSocketErrorEventArgs {
                    Exception = ex
                });
                OnClose?.Invoke(this, new WebSocketCloseEventArgs());
                webSocket.Dispose();
            }
        }

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        private async Task ReceiveAsync() {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            var messageBuffer = new List<byte>();

            try {
                while (State == WebSocketState.Open) {
                    WebSocketReceiveResult result;
                    do {
                        // No lock needed, as this method can't be called directly.
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        messageBuffer.AddRange(buffer.Array!.Take(result.Count));
                    }
                    while (!result.EndOfMessage);

                    switch (result.MessageType) {
                        case System.Net.WebSockets.WebSocketMessageType.Text:
                            OnMessage?.Invoke(this, new WebSocketMessageEventArgs {
                                Data = messageBuffer.ToArray(),
                                MessageType = WebSocketMessageType.Text
                            });
                            break;
                        case System.Net.WebSockets.WebSocketMessageType.Binary:
                            OnMessage?.Invoke(this, new WebSocketMessageEventArgs {
                                Data = messageBuffer.ToArray(),
                                MessageType = WebSocketMessageType.Text
                            });
                            break;
                        case System.Net.WebSockets.WebSocketMessageType.Close:
                            await CloseAsync();
                            OnClose?.Invoke(this, new WebSocketCloseEventArgs {
                                CloseStatus = (WebSocketCloseStatus) result.CloseStatus!,
                                CloseStatusDescription = result.CloseStatusDescription
                            });
                            break;
                    }

                    messageBuffer.Clear();
                }
            }
            catch (Exception ex) {
                OnError?.Invoke(this, new WebSocketErrorEventArgs {
                    Exception = ex
                });
                OnClose?.Invoke(this, new WebSocketCloseEventArgs() {
                    CloseStatus = WebSocketCloseStatus.Empty
                });
            }
            finally {
                webSocket.Dispose();
            }
        }

        public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? statusDescription = null) {
            if(State == WebSocketState.Open) {
                await webSocket.CloseAsync((System.Net.WebSockets.WebSocketCloseStatus) closeStatus, statusDescription ?? string.Empty, CancellationToken.None);
            }
        }

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">When invoked in state other than Open.</exception>
        public async Task SendAsync(IEnumerable<byte> bytes) {
            await SendAsync(bytes.ToArray(), System.Net.WebSockets.WebSocketMessageType.Binary);
        }

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">When invoked in state other than Open.</exception>
        public async Task SendAsync(byte[] bytes) {
            await SendAsync(bytes, System.Net.WebSockets.WebSocketMessageType.Binary);
        }

        /// <summary>
        /// <inheritdoc cref="IWebSocket"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">When invoked in state other than Open.</exception>
        public async Task SendAsync(string text) {
            var bytes = Encoding.UTF8.GetBytes(text);
            await SendAsync(bytes, System.Net.WebSockets.WebSocketMessageType.Text);
        }

        public async Task SendAsync(ArraySegment<byte> bytes, System.Net.WebSockets.WebSocketMessageType messageType) {
            if (State != WebSocketState.Open) {
                throw new InvalidOperationException("SendAsync can only be invoked in Open state.");
            }

            QueueMessage message = new QueueMessage(bytes, messageType);

            // Parallel send is undefined behavior.
            // If there is a pending send then we enqueue the message,
            // which gets processed after the current send finishes.
            lock(sendMessageLock) {
                if(!sendingMessage) {
                    sendingMessage = true;
                }
                else {
                    messageQueue.Enqueue(message);
                    return;
                }
            }

            try {
                await webSocket.SendAsync(message.Bytes, message.Type, true, CancellationToken.None);

                while(true) {
                    QueueMessage nextMessage;
                    lock(sendMessageLock) {
                        if(messageQueue.Count > 0) {
                            nextMessage = messageQueue.Dequeue();
                        }
                        else {
                            sendingMessage = false;
                            return;
                        }
                    }

                    await webSocket.SendAsync(nextMessage.Bytes, nextMessage.Type, true, CancellationToken.None);
                }
            }
            catch(Exception ex) {
                OnError?.Invoke(this, new WebSocketErrorEventArgs { Exception = ex });
                lock(sendMessageLock) {
                    sendingMessage = false;
                }
            }

        }
    }
}