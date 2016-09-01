using System;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Websockets.Universal
{
    /// <summary>
    /// A Websocket connection for Universal
    /// </summary>
    public class WebsocketConnection : IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<IWebSocketConnection> OnDispose = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<string> OnLog = delegate { };

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnection());
        }

        private MessageWebSocket _websocket;
        private DataWriter messageWriter;

        public void Open(string url, string protocol = null)
        {
            try
            {
                if (_websocket != null)
                    EndConnection();

                _websocket = new MessageWebSocket();
                _websocket.Control.MessageType = SocketMessageType.Utf8;
                _websocket.Closed += _websocket_Closed;
                _websocket.MessageReceived += _websocket_MessageReceived;

                if (url.StartsWith("https"))
                    url = url.Replace("https://", "wss://");
                else if (url.StartsWith("http"))
                    url = url.Replace("http://", "ws://");

                Uri uri = new Uri(url);
                int port = uri.Port.ToString();
                _websocket.ConnectAsync(uri, port).Completed = (source, status) =>
                {
                    if (status == AsyncStatus.Completed)
                    {
                        messageWriter = new DataWriter(_websocket.OutputStream);
                        IsOpen = true;
                        OnOpened();
                    }
                    else if (status == AsyncStatus.Error)
                    {
                        OnError("Websocket error");
                    }
                };


            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }


        public void Close()
        {
            EndConnection();
        }

        public async void Send(string message)
        {
            if (_websocket != null && messageWriter != null)
            {
                try
                {
                    messageWriter.WriteString(message);
                    await messageWriter.StoreAsync();
                }
                catch
                {
                    OnError("Failed to send message.");
                }
            }
        }


        public void Dispose()
        {
            Close();
            OnDispose(this);
        }

        void EndConnection()
        {
            if (_websocket != null)
            {
                _websocket.Dispose();
                _websocket = null;

                IsOpen = false;
                OnClosed();
            }
        }
        void _websocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (var reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    var text = reader.ReadString(reader.UnconsumedBufferLength);
                    OnMessage(text);
                }
            }
            catch
            {
                OnError("Failed to read message.");
            }
        }

        void _websocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            IsOpen = false;
            OnClosed();
        }
    }
}
