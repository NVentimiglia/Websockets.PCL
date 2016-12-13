using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Websockets.Net
{
    internal class WebSocketWrapper : IDisposable
    {
        public event Action<WebSocketWrapper> Opened;
        public event Action<string, WebSocketWrapper> MessageReceived;
        public event Action<WebSocketWrapper> Closed;
        public event Action<Exception> Error;

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private ClientWebSocket _ws;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private AsyncLock _asyncLock = new AsyncLock();

        public bool IsDisposed { get; set; }

        public WebSocketWrapper()
        {
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _cancellationToken = _cancellationTokenSource.Token;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <returns></returns>
        public static WebSocketWrapper Create()
        {
            return new WebSocketWrapper();
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        public async Task Connect(string uri, string protocol = null, IDictionary<string, string> headers = null)
        {
			if (protocol != null)
			{
				_ws.Options.AddSubProtocol(protocol);
			}
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _ws.Options.SetRequestHeader(header.Key, header.Value);
                }
            }

			await _ws.ConnectAsync(new Uri(uri), _cancellationToken);
            CallOnConnected();
            StartListen();
        }

        public async Task Disconnect()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                //Close Async not working, never ending await
                await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception ex)
            {
                // DisposedObjectException, every time
              //  CallOnError(ex);
            }
        }

        /// <summary>
        /// Send a message to the WebSocket server.
        /// </summary>
        /// <param name="message">The message to send</param>
        public async Task SendMessage(string message)
        {
            if (_ws.State != WebSocketState.Open)
            {
                if (Error != null)
                    Error(new Exception("WebSocket:Send : Connection is not open."));
                return;
            }
            try
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);
                using (await _asyncLock.LockAsync())
                {
                    await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, 0, messageBuffer.Length), WebSocketMessageType.Text, true, _cancellationToken);
                }
            }
            catch (Exception ex)
            {
                CallOnError(ex);
            }
        }


        private async void StartListen()
        {
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close ||
                            result.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            CallOnDisconnected();
                            Dispose();

                            return;
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);
                    CallOnMessage(stringResult);
                }
            }
            catch (Exception ex)
            {
                CallOnDisconnected();
            }
        }

        private void CallOnMessage(StringBuilder stringResult)
        {
            var ev = MessageReceived;
            if (ev != null)
                RunInTask(() => ev(stringResult.ToString(), this));
        }

        private void CallOnDisconnected()
        {
            var ev = Closed;
            if (ev != null)
                RunInTask(() => ev(this));
        }

        private void CallOnConnected()
        {
            var ev = Opened;
            if (ev != null)
                RunInTask(() => ev(this));
        }

        private void CallOnError(Exception ex)
        {
            var ev = Error;
            if (ev != null)
                RunInTask(() => ev(ex));
        }

        private static void RunInTask(Action action)
        {
            Task.Factory.StartNew(action);
        }

        public void Dispose()
        {
            if (_ws != null)
            {
                _cancellationTokenSource.Cancel();
                _ws.Dispose();
                _ws = null;
            }
        }
    }
}