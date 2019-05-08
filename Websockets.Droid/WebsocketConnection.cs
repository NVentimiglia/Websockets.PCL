using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Websockets.DroidBridge;

namespace Websockets.Droid
{
    /// <summary>
    /// A Websocket connection via androidBridge application
    /// </summary>
    public class WebsocketConnection : BridgeProxy, IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<IWebSocketConnection> OnDispose = delegate { };
        public event Action<Exception> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<byte[]> OnData = delegate { };
        public event Action<string> OnLog = delegate { };

        private BridgeController _controller;
        private static bool _isAllTrusted = false;

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnection());
        }

        public void Close()
        {
            try
            {
                IsOpen = false;
                _controller.Close();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

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
            try
            {
                _controller = new BridgeController
                {
                    Proxy = this
                };
                if (_isAllTrusted)
                {
                    _controller.SetIsAllTrusted();
                }
                _controller.Open(url, protocol, headers);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                Close();
                OnDispose(this);
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }


        public void Send(string message)
        {
            try
            {
                _controller.Send(message);

            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                _controller.Send(data);

            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void SetIsAllTrusted()
        {
            if (!_isAllTrusted) {
                _isAllTrusted = true;
            }
        }

        //

        public override unsafe void RaiseClosed()
        {
            IsOpen = false;
            OnClosed();
            base.RaiseClosed();
        }

        public override unsafe void RaiseError(string p1)
        {
            OnError(new Exception(p1));
            base.RaiseError(p1);
        }

        public override unsafe void RaiseError(Java.Lang.Exception p1)
        {
            OnError(p1);
            base.RaiseError(p1);
        }

        public override unsafe void RaiseLog(string p1)
        {
            OnLog(p1);
            base.RaiseLog(p1);
        }

        public override unsafe void RaiseMessage(string p1)
        {
            OnMessage(p1);
            base.RaiseMessage(p1);
        }

        public override unsafe void RaiseData(byte[] p1)
        {
            OnData(p1);
            base.RaiseData(p1);
        }

        public override unsafe void RaiseOpened()
        {
            IsOpen = true;
            OnOpened();
            base.RaiseOpened();
        }
    }
}