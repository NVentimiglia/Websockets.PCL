using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace Websockets.NetTests
{
    public class TestSample
    {
        private Websockets.IWebSocketConnection connection;
        private bool Failed;
        private bool Echo;
        
        public void Setup()
        {
            // 1) Link in your main activity
            //Websockets.Net.WebsocketConnection.Link();
        }

        
        public async Task DoTest()
        {
            // 2) Call factory from your PCL code.
            // This is the same as new   Websockets.Droid.WebsocketConnectionDroid();
            // Except that the Factory is in a PCL and accessible anywhere
            connection = Websockets.WebSocketFactory.Create();
            connection.SetIsAllTrusted();
            connection.OnLog += Connection_OnLog;
            connection.OnError += Connection_OnError;
            connection.OnMessage += Connection_OnMessage;
            connection.OnOpened += Connection_OnOpened;
            connection.OnClosed += Connection_OnClosed;
            connection.OnDispose += Connection_OnDispose;

            //Timeout / Setup
            Echo = Failed = false;
            var token = new CancellationTokenSource();
            Timeout(token.Token);

            //Do test

            Debug.WriteLine("Connecting...");

            connection.Open(Program.WSECHOD_URL);

            while (!connection.IsOpen && !Failed)
            {
                await Task.Delay(10);
            }

            if (!connection.IsOpen)
            {
                token.Cancel();
                Assert.True(false);
                return;
            }
            Debug.WriteLine("Connected !");

            Debug.WriteLine("Sending...");

            connection.Send("Hello World");

            Debug.WriteLine("Sent !");

            while (!Echo && !Failed)
            {
                await Task.Delay(10);
            }

            if (!Echo)
            {
                token.Cancel();
                connection.Dispose();
                Assert.True(Echo);
                return;
            }

            token.Cancel();
            connection.Dispose();

            Debug.WriteLine("Received !");

            Debug.WriteLine("Passed !");
            Assert.True(true);
        }

        private void Connection_OnOpened()
        {
            Debug.WriteLine("Opened !");
        }

        private void Connection_OnClosed()
        {
            Debug.WriteLine("Closed !");
        }

        async void Timeout(CancellationToken token)
        {
            try
            {
                var t = Task.Delay(30000, token);
                await t;
                if (!t.IsCanceled)
                {
                    Debug.WriteLine("Timeout");
                    Failed = true;
                }
            }
            catch (TaskCanceledException) { }
        }

        private void Connection_OnMessage(string obj)
        {
            Echo = obj == "Hello World";
        }
        
        private void Connection_OnError(Exception ex)
        {
            Trace.WriteLine("ERROR " + ex.ToString());
            Failed = true;
        }

        private void Connection_OnLog(string obj)
        {
            Trace.WriteLine(obj);
        }

        private void Connection_OnDispose(IWebSocketConnection c)
        {
            Trace.WriteLine(GetType().ToString() + " dispose");
        }

    }
}