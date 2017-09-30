using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public event Action<byte[], WebSocketWrapper> DataReceived;
        public event Action<WebSocketWrapper> Closed;
        public event Action<Exception> Error;

        private const int ReceiveChunkSize = 1024;

        private ClientWebSocket _ws;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private AsyncLock _asyncLock = new AsyncLock();

        public bool IsDisposed { get; set; }

        public WebSocketWrapper()
        {
            _ws = new ClientWebSocket();
            _ws.Options.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();
            _ws.Options.UseDefaultCredentials = false;
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

            await _ws.ConnectAsync(new Uri(uri), CancellationToken.None); // no cancellation because we get state aborted
            CallOnConnected();
            StartReceive();
        }

        public void Disconnect()
        {
            try
            {
                if (_cancellationTokenSource.Token.CanBeCanceled)
                {
                    _cancellationTokenSource.Cancel();
                }

                if (_ws.State == WebSocketState.Open)
                {
                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "1", CancellationToken.None).Wait(); // no cancellation because we get state aborted
                }
                else
                {
                    Debug.WriteLine("wsState: " + _ws.State.ToString());
                }
                Debug.WriteLine("Disconnect()");
            }
            catch (AggregateException ex)
            {
                ex.Handle((iex) =>
                {
                    Debug.WriteLine(string.Format("{0}: {1}: {2}", iex.GetType().ToString(), iex.Message, iex.StackTrace));
                    return true;
                });
            }
            catch (Exception ex)
            {
                // DisposedObjectException, every time, yeah but where does it come from?
                //  CallOnError(ex);
                Debug.WriteLine(string.Format("{0}: {1}: {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace));
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
                Error?.Invoke(new Exception("WebSocket: Send: Connection is not open."));
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

        /// <summary>
        /// Send binary data to the WebSocket server.
        /// </summary>
        /// <param name="data">The data to send</param>
        public async Task SendData(byte[] data)
        {
            if (_ws.State != WebSocketState.Open)
            {
                Error?.Invoke(new Exception("WebSocket: SendData: Connection is not open."));
                return;
            }
            try
            {
                using (await _asyncLock.LockAsync())
                {
                    await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, _cancellationToken);
                }
            }
            catch (Exception ex)
            {
                CallOnError(ex);
            }
        }


        private async void StartReceive()
        {
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    byte[] data = new byte[0];
                    var totallen = 0;

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None); // no cancellation because we get state aborted

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "2", CancellationToken.None); // no cancellation because we get state aborted
                            CallOnDisconnected();
                            Dispose();

                            return;
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            var data2 = new byte[totallen + result.Count];
                            data.CopyTo(data2, 0);
                            for (int i = 0; i < result.Count; i++)
                            {
                                data2[totallen + i] = buffer[i];
                            }
                            data = data2;
                            totallen += result.Count;
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        CallOnMessage(stringResult);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        CallOnData(data);
                    }
                }
            }
            catch (Exception ex)
            {
                CallOnError(ex);
                CallOnDisconnected();
            }
        }

        private void CallOnMessage(StringBuilder stringResult)
        {
            var ev = MessageReceived;
            if (ev != null)
                RunInTask(() => ev(stringResult.ToString(), this));
        }

        private void CallOnData(byte[] data)
        {
            var ev = DataReceived;
            if (ev != null)
                RunInTask(() => ev(data, this));
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
                Disconnect();
                _ws = null;
            }
        }
    }
}