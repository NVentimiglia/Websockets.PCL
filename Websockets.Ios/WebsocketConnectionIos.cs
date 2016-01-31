using System;
using Foundation;
using Square.SocketRocket;


namespace Websockets.Ios
{
    [Preserve]
    public class WebsocketConnectionIos : IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<string> OnLog = delegate { };

        static WebsocketConnectionIos()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        }

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnectionIos());
        }
        
        private WebSocket _client = null;


        public void Open(string url, string protocol = null)
        {
            try
            {
                if(_client != null)
                    Close();

                if (string.IsNullOrEmpty(protocol))
                    _client = new WebSocket(new NSUrl(url));
                else
                    _client = new WebSocket(new NSUrl(url), new NSObject[] { new NSString(protocol) });

                _client.ReceivedMessage += _client_ReceivedMessage;
                _client.WebSocketClosed += _client_WebSocketClosed;
                _client.WebSocketFailed += _client_WebSocketFailed;
                _client.WebSocketOpened += _client_WebSocketOpened;

                _client.Open();
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        public void Close()
        {
            try
            {
                if (_client != null)
                {
                    _client.ReceivedMessage -= _client_ReceivedMessage;
                    _client.WebSocketClosed -= _client_WebSocketClosed;
                    _client.WebSocketFailed -= _client_WebSocketFailed;
                    _client.WebSocketOpened -= _client_WebSocketOpened;

                    if (_client.ReadyState == ReadyState.Open)
                    {
                        _client.Close();
                    }

                    _client.Dispose();
                    _client = null;

                    var ev = OnClosed;
                    if (ev != null)
                    {
                        ev();
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        public void Send(string message)
        {
            _client.Send(new NSString(message));
        }

        public void Dispose()
        {
            Close();
        }


        // Handlers


        private void _client_WebSocketOpened(object sender, EventArgs e)
        {
            OnOpened();
        }

        private void _client_WebSocketFailed(object sender, WebSocketFailedEventArgs e)
        {

            if (e.Error != null)
                OnError(e.Error.Description);
            else
                OnError("Unknown WebSocket Error!");

            OnClosed();
        }

        private void _client_WebSocketClosed(object sender, WebSocketClosedEventArgs e)
        {
            OnClosed();
        }

        private void _client_ReceivedMessage(object sender, WebSocketReceivedMessageEventArgs e)
        {
            OnMessage(e.Message.ToString());
        }
    }
}