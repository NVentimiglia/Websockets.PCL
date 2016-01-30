using System;

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
                throw  new Exception("Websocket factory is not initialized !");
            }

            return factoryMethod();
        }
    }
}