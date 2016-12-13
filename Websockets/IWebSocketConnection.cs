using System;
using System.Collections.Generic;

namespace Websockets
{
    /// <summary>
    /// WebSocket contract.
    /// </summary>
    public interface IWebSocketConnection : IDisposable
    {
        bool IsOpen { get; }

        void Open(string url, string protocol = null, string authToken = null);

        void Open(string url, string protocol, IDictionary<string, string> headers);

        void Close();

        void Send(string message);
        
        event Action OnOpened;

        event Action OnClosed;

        event Action<IWebSocketConnection> OnDispose;

        event Action<string> OnError;

        event Action<string> OnMessage;
        
        event Action<string> OnLog;
    }
}
