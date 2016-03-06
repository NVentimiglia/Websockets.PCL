using System;
using System.Collections.Generic;

namespace Websockets
{
    /// <summary>
    /// Static Factory for getting WebSocket Connection instances
    /// </summary>
    /// <remarks>
    /// Must be Init by platform code
    /// </remarks>
    public static class WebSocketFactory
    {
        static Func<IWebSocketConnection> factoryMethod;

        static List<IWebSocketConnection> _connections = new List<IWebSocketConnection>();

        /// <summary>
        /// Call from platform code. e.g. : Websockets.Droid.Platform.Init();
        /// </summary>
        /// <param name="factory"></param>
        public static void Init(Func<IWebSocketConnection> factory)
        {
            factoryMethod = factory;
        }

        /// <summary>
        /// Returns a new websocket instance
        /// </summary>
        /// <returns></returns>
        public static IWebSocketConnection Create()
        {
            if (factoryMethod == null)
            {
                throw new Exception("Websocket factory is not initialized !");
            }

            var client = factoryMethod();
            _connections.Add(client);
            client.OnDispose += Client_OnDispose;
            return client;
        }

        private static void Client_OnDispose(IWebSocketConnection connection)
        {
            _connections.Remove(connection);
        }

        /// <summary>
        /// Cleanup helper
        /// </summary>
        public static void DisposeAll()
        {
            InvokeOnAll(client =>
            {
                client.Dispose();
            });
        }

        /// <summary>
        /// Invokes an action on all Websocket connections
        /// </summary>
        /// <remarks>
        /// Dispose on Terminate ?
        /// </remarks>
        /// <param name="action"></param>
        public static void InvokeOnAll(Action<IWebSocketConnection> action)
        {
            var connections = _connections.ToArray();
            foreach (var connection in connections)
            {
                action(connection);
            }
        }

        /// <summary>
        /// Returns all non disposed connections
        /// </summary>
        /// <remarks>
        /// Dispose on Terminate ?
        /// </remarks>
        /// <returns></returns>
        public static IEnumerable<IWebSocketConnection> Connections()
        {
            return _connections.ToArray();
        }
    }
}