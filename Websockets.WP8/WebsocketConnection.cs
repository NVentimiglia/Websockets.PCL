using System;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using System.Collections.Generic;

namespace Websockets.WP8
{
    /// <summary>
    /// A Websocket connection 
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

        protected WebSocket websocket;

        public void Open(string url, string protocol = null, string authToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (authToken != null)
            {
                headers.Add("Authorization", authToken);
            }

            Open(url, protocol, headers);
        }

        public void Open(string url, string protocol, IDictionary<string, string> headers)
        {
            Close();

            if (url.StartsWith("https"))
                url = url.Replace("https://", "wss://");
            else if (url.StartsWith("http"))
                url = url.Replace("http://", "ws://");

            if (headers != null && headers.Count > 0)
            {
                throw new NotImplementedException();
                //foreach (var entry in headers)
                //{
                //    _websocket.SetRequestHeader(entry.Key, entry.Value);
                //}
            }

            websocket = new WebSocket(url, protocol);
            websocket.Opened += Websocket_Opened;
            websocket.Error += Websocket_Error;
            websocket.Closed += Websocket_Closed;
            websocket.MessageReceived += Websocket_MessageReceived;
            websocket.Open();
        }
        
        public void Send(string message)
        {
            websocket.Send(message);
        }

        public void Close()
        {
            IsOpen = false;
            if (websocket != null)
            {
                websocket.Close();

                websocket.Dispose();

                websocket = null;
            }
        }

        public void Dispose()
        {
            Close();
            OnDispose(this);
        }

        //

        private void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            OnMessage(e.Message);
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            IsOpen = false;
            OnClosed();
        }

        private void Websocket_Error(object sender, ErrorEventArgs e)
        {
            OnError(e.Exception.Message);
        }

        private void Websocket_Opened(object sender, EventArgs e)
        {
            IsOpen = true;
            OnOpened();
        }
    }
}