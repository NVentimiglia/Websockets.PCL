using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
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
        public event Action<Exception> OnError = delegate { };
        public event Action<string> OnMessage = delegate { };
        public event Action<byte[]> OnData = delegate { };
        public event Action<string> OnLog = delegate { };

        /// <summary>
        /// Factory Initializer
        /// </summary>
        public static void Link()
        {
            WebSocketFactory.Init(() => new WebsocketConnection());
        }

        private MessageWebSocket _websocket;
        private bool _isAllTrusted = false;
        private DataWriter messageWriter;

        public void Open(string url, string protocol = null, string authToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
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
                if (_websocket != null)
                    EndConnection();

                _websocket = new MessageWebSocket();
                if (_isAllTrusted)
                {
                    // https://docs.microsoft.com/en-us/uwp/api/windows.networking.sockets.messagewebsocketcontrol#Properties
                    // IgnorableServerCertificateErrors
                    // introduced: Windows 10 Anniversary Edition (introduced v10.0.14393.0,
                    //   Windows.Foundation.UniversalApiContract (introduced v3)
                    try
                    {
                        dynamic info = _websocket.Information;
                        info.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                        info.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("not supported: {0}: {1}: {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace));
                    }
                }

                _websocket.Closed += _websocket_Closed;
                _websocket.MessageReceived += _websocket_MessageReceived;

                if (url.StartsWith("https"))
                    url = url.Replace("https://", "wss://");
                else if (url.StartsWith("http"))
                    url = url.Replace("http://", "ws://");

                if (headers != null)
                {
                    foreach (var entry in headers)
                    {
                        _websocket.SetRequestHeader(entry.Key, entry.Value);
                    }
                }

                _websocket.ConnectAsync(new Uri(url)).Completed = (source, status) =>
                {
                    if (status == AsyncStatus.Completed)
                    {
                        messageWriter = new DataWriter(_websocket.OutputStream);
                        IsOpen = true;
                        OnOpened();
                    }
                    else if (status == AsyncStatus.Error)
                    {
                        OnError(new Exception("Websocket error"));
                    }
                };


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
            if (_websocket != null && messageWriter != null)
            {
                try
                {
                    _websocket.Control.MessageType = SocketMessageType.Utf8;
                    messageWriter.WriteString(message);
                    await messageWriter.StoreAsync();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public async void Send(byte[] data)
        {
            if (_websocket != null && messageWriter != null)
            {
                try
                {
                    _websocket.Control.MessageType = SocketMessageType.Binary;
                    messageWriter.WriteBytes(data);
                    await messageWriter.StoreAsync();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void SetIsAllTrusted()
        {
            if (!_isAllTrusted)
            {
                _isAllTrusted = true;
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
                    if (args.MessageType == SocketMessageType.Utf8)
                    {
                        reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                        var text = reader.ReadString(reader.UnconsumedBufferLength);
                        OnMessage(text);
                    }
                    else if (args.MessageType == SocketMessageType.Binary)
                    {
                        var buflen = reader.UnconsumedBufferLength;
                        buflen = (buflen > int.MaxValue || (int)buflen < 0 ? int.MaxValue : buflen);
                        var data = new byte[buflen];
                        var totallen = 0;
                        while (buflen > 0)
                        {
                            var buf = new byte[buflen];
                            reader.ReadBytes(buf);
                            if (totallen + buflen > data.Length)
                            {
                                var data2 = new byte[totallen + buflen];
                                data.CopyTo(data2, 0);
                                data = data2;
                            }
                            buf.CopyTo(data, totallen);
                            totallen += (int)buflen;
                            buflen = reader.UnconsumedBufferLength;
                            buflen = (buflen > int.MaxValue || (int)buflen < 0 ? int.MaxValue : buflen);
                        }
                        OnData(data);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        void _websocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            IsOpen = false;
            OnClosed();
        }
    }
}