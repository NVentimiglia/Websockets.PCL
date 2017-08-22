using System;
using Foundation;
using Square.SocketRocket;
using System.Collections.Generic;

namespace Websockets.Ios
{
    [Preserve]
    public class WebsocketConnection : IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<IWebSocketConnection> OnDispose = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<string> OnLog = delegate { };

        static WebsocketConnection()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        }

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnection());
        }

        private WebSocket _client = null;

        public void Open(string url, string protocol = null, string authToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (authToken != null)
            {
                headers.Add("Authorization", authToken);
            }

            Open(url, protocol, headers);
        }

        public void Open(string url, string protocol, IDictionary<string, string> headers = null)
        {
            try
            {
                if (_client != null)
                    Close();

                NSUrlRequest req = new NSUrlRequest(new NSUrl(url));
                if (headers?.Count > 0)
                {
                    NSMutableUrlRequest mutableRequest = new NSMutableUrlRequest(new NSUrl(url));
                    foreach (var header in headers)
                    {
                        mutableRequest[header.Key] = header.Value;
                    }
                    req = (NSUrlRequest)mutableRequest.Copy();
                }

                if (string.IsNullOrEmpty(protocol))
                    _client = new WebSocket(req);
                else
                    _client = new WebSocket(req, new NSObject[] { new NSString(protocol) });

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
					IsOpen = false;
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
            try
            {
                if (_client != null)
                    _client.Send(new NSString(message));
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        public void Dispose()
        {
            Close();
            OnDispose(this);
        }


        // Handlers


        private void _client_WebSocketOpened(object sender, EventArgs e)
        {
            IsOpen = true;
            OnOpened();
        }

        private void _client_WebSocketFailed(object sender, WebSocketFailedEventArgs e)
        {
			bool wasOpen = IsOpen;
			IsOpen = false;
            if (e.Error != null)
                OnError(e.Error.Description);
            else
                OnError("Unknown WebSocket Error!");

			if (wasOpen)
            {
                OnClosed();
            }
        }

        private void _client_WebSocketClosed(object sender, WebSocketClosedEventArgs e)
        {
            IsOpen = false;
            OnClosed();
        }

        private void _client_ReceivedMessage(object sender, WebSocketReceivedMessageEventArgs e)
        {
            if (e != null && e.Message != null)
                OnMessage(e.Message.ToString());
        }
    }
}
