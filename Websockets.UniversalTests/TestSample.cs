using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace Websockets.UniversalTests
{
    public class TestSample
    {
        public static readonly string WSECHOD_URL = "wss://wsecho.n4v.eu";
        //public static readonly string WSECHOD_URL = "wss://localhost:8080";

        private Websockets.IWebSocketConnection connection;
        private bool Failed;
        private bool Echo;
        
        public void Setup()
        {
            // 1) Link in your main activity
            //Websockets.Universal.WebsocketConnection.Link();
        }

        
        public async void DoTest()
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

            //Timeout / Setup
            Echo = Failed = false;
            var token = new CancellationTokenSource();
            Timeout(token.Token);

            //Do test

            Debug.WriteLine("Connecting...");
            connection.Open(WSECHOD_URL);

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
            Debug.WriteLine("ERROR " + ex.Message);
            Failed = true;
        }

        private void Connection_OnLog(string obj)
        {
            Debug.WriteLine(obj);
        }
    }
}