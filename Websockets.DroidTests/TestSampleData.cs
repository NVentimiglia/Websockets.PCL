using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Threading;

namespace Websockets.DroidTests
{
    [TestFixture]
    public class TestSampleData
    {
        private Websockets.IWebSocketConnection connection;
        private bool Failed;
        private bool Echo;

        [SetUp]
        public void Setup()
        {
            // 1) Link in your main activity
            //Websockets.Droid.WebsocketConnection.Link();
        }

        [Test]
        public async void DoTest()
        {
            // 2) Call factory from your PCL code.
            // This is the same as new   Websockets.Droid.Websocketconnection();
            // Except that the Factory is in a PCL and accessible anywhere
            connection = Websockets.WebSocketFactory.Create();
            connection.SetIsAllTrusted();
            connection.OnLog += Connection_OnLog;
            connection.OnError += Connection_OnError;
            connection.OnMessage += Connection_OnMessage;
            connection.OnData += Connection_OnData;
            connection.OnOpened += Connection_OnOpened;

            //Timeout / Setup
            Echo = Failed = false;
            var token = new CancellationTokenSource();
            Timeout(token.Token);

            //Do test

            Debug.WriteLine("Connecting...");

            connection.Open(TestSample.WSECHOD_URL);

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

            var data = new byte[] { 0, (byte)'H', (byte)'I' };
            connection.Send(data);

            Debug.WriteLine("Sent !");

            while (!Echo && !Failed)
            {
                await Task.Delay(10);
            }

            if (!Echo)
            {
                token.Cancel();
                Assert.True(Echo);
                return;
            }

            token.Cancel();

            Debug.WriteLine("Received !");

            Debug.WriteLine("Passed !");
            Trace.WriteLine("Passed");
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

        private void Connection_OnData(byte[] data)
        {
            Echo = false;
            var compare = new byte[] { 0, (byte)'H', (byte)'I' };
            if (data.Length == compare.Length)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] != compare[i])
                    {
                        return;
                    }
                }
                Echo = true;
                return;
            }
        }

        private void Connection_OnError(Exception obj)
        {
            Trace.WriteLine("ERROR " + obj.ToString());
            Failed = true;
        }

        private void Connection_OnLog(string obj)
        {
            Trace.WriteLine(obj);
        }

        [TearDown]
        public void Tear()
        {
            if (connection != null)
            {
                connection.Dispose();
            }
        }
    }
}