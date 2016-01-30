using System;
using Android.App;
using Android.OS;
using Websockets.DroidBridge;

namespace Websockets.Droid
{
    /// <summary>
    /// A Websocket connection via androidBridge application
    /// </summary>
    public class WebsocketConnectionDroid : BridgeProxy, IWebSocketConnection
    {
        public bool IsOpen { get; private set; }

        public event Action OnClosed = delegate { };
        public event Action OnOpened = delegate { };
        public event Action<string> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<string> OnLog = delegate { };

        private readonly BridgeController _controller;
       private Handler handler;

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnectionDroid());
        }

        public WebsocketConnectionDroid()
        {
            _controller = new BridgeController();
            _controller.Proxy = this;
        }


        public void Close()
        {
            IsOpen = false;
            _controller.Close();
        }

        public void Open(string url, string protocol = null)
        {
            _controller.Proxy = this;
            _controller.Proxy = this;
            _controller.Open(url, protocol);
        }

        protected override void Dispose(bool disposing)
        {
            Close();
            base.Dispose(disposing);
        }


        public void Send(string message)
        {
            _controller.Send(message);
        }

        //

        public override unsafe void RaiseClosed()
        {
            base.RaiseClosed();
            OnClosed();
        }

        public override unsafe void RaiseError(string p1)
        {
            base.RaiseError(p1);
            OnError(p1);
        }

        public override unsafe void RaiseLog(string p1)
        {
            base.RaiseLog(p1);
            OnLog(p1);
        }

        public override unsafe void RaiseMessage(string p1)
        {
            base.RaiseMessage(p1);
            OnMessage(p1);
        }

        public override unsafe void RaiseOpened()
        {
            IsOpen = true;
            base.RaiseOpened();
            OnOpened();
        }
    }
}