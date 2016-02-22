using System;
using System.Diagnostics;
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
        public event Action<string> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<byte[]> OnData = delegate { };
        public event Action<string> OnLog = delegate { };
        public event Action<IWebSocketConnection> OnDispose = delegate { };

        private BridgeController _controller;

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

        public void Close()
        {
            try
            {
                IsOpen = false;
                _controller.Close();
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        public void Open(string url, string protocol = null)
        {
            try
            {
                _controller = new BridgeController();
                _controller.Proxy = this;
                _controller.Open(url, protocol);
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                OnDispose(this);
                Close();
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }


        public void Send(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                _controller.Send(message);
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        //

        public override unsafe void RaiseClosed()
        {
            try
            {
                OnClosed();
                base.RaiseClosed();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override unsafe void RaiseError(string p1)
        {
            try
            {
                OnError(p1);
                base.RaiseError(p1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override unsafe void RaiseLog(string p1)
        {
            try
            {
                OnLog(p1);
                base.RaiseLog(p1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override unsafe void RaiseMessage(string p1)
        {
            try
            {
                OnMessage(p1);
                base.RaiseMessage(p1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public override unsafe void RaiseOpened()
        {
            try
            {
                IsOpen = true;
                OnOpened();
                base.RaiseOpened();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}