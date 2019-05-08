using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Websockets.Net
{
    /// <summary>
    /// A Websocket connection for 'full' .Net Core applications
    /// </summary>
    public class WebsocketConnection : IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<IWebSocketConnection> OnDispose = delegate { };
        public event Action<Exception> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<byte[]> OnData = delegate { };
        public event Action<string> OnLog = delegate { };

        static bool AllTrustedValidationCallback(object req, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            Debug.WriteLine("protocol: " + ((HttpWebRequest)req).ProtocolVersion.ToString());
            Debug.WriteLine("cert: " + certificate.Subject);
            foreach (X509ChainElement el in chain.ChainElements)
            {
                Debug.WriteLine("chain: " + el.Certificate.Subject);
            }
            Debug.WriteLine("policyerror: " + (errors.ToString()));
            return true;
        }

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnection());
        }

        private WebSocketWrapper _websocket = null;
        private static bool _isAllTrusted = false;

        public void Open(string url, string protocol = null, string authToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (authToken != null)
            {
                headers.Add("Authorization", authToken);
            }

            Open(url, protocol, headers);
        }

        public async void Open(string url, string protocol, IDictionary<string, string> headers = null)
        {
            try
            {
                if (_websocket != null)
                    EndConnection();

                _websocket = new WebSocketWrapper();
                _websocket.Closed += _websocket_Closed;
                _websocket.Opened += _websocket_Opened;
                _websocket.Error += _websocket_Error;
                _websocket.MessageReceived += _websocket_MessageReceived;
                _websocket.DataReceived += _websocket_DataReceived;

                if (url.StartsWith("https"))
                    url = url.Replace("https://", "wss://");
                else if (url.StartsWith("http"))
                    url = url.Replace("http://", "ws://");

                await _websocket.Connect(url, protocol, headers);

            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void Close()
        {
            EndConnection();
        }

        public async void Send(string message)
        {
            await _websocket.SendMessage(message);
        }

        public async void Send(byte[] data)
        {
            await _websocket.SendData(data);
        }

        public void Dispose()
        {
            Close();
            OnDispose(this);
        }

        public void SetIsAllTrusted()
        {
            if (!_isAllTrusted)
            {
                _isAllTrusted = true;
                ServicePointManager.ServerCertificateValidationCallback += AllTrustedValidationCallback;
            }
        }

        //
        void EndConnection()
        {
            if (_websocket != null)
            {
                _websocket.Closed -= _websocket_Closed;
                _websocket.Opened -= _websocket_Opened;
                _websocket.Error -= _websocket_Error;
                _websocket.MessageReceived -= _websocket_MessageReceived;
                _websocket.DataReceived -= _websocket_DataReceived;
                _websocket.Dispose();
                _websocket = null;

                IsOpen = false;
                OnClosed();
            }
        }


        void _websocket_Error(Exception ex)
        {
            OnError(ex);
        }

        void _websocket_Opened(WebSocketWrapper arg)
        {

            IsOpen = true;
            OnOpened();
        }

        void _websocket_MessageReceived(string m, WebSocketWrapper arg)
        {

            OnMessage(m);
        }

        void _websocket_DataReceived(byte[] data, WebSocketWrapper arg)
        {

            OnData(data);
        }

        void _websocket_Closed(WebSocketWrapper arg)
        {
            EndConnection();
        }
    }
}