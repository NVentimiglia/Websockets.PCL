using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;


namespace Websockets.DroidTests
{
    [TestFixture]
    public class TestsSample
    {
        private Websockets.IWebSocketConnection connection;
        private bool Failed;
        private bool Echo;

        [SetUp]
        public void Setup()
        {
            // 1) Link in your main activity
            Websockets.Droid.WebsocketConnection.Link();
        }


        [Test]
        public async void DoTest()
        {
            // 2) Call factory from your PCL code.
            // This is the same as new   Websockets.Droid.WebsocketConnection();
            // Except that the Factory is in a PCL and accessible anywhere
            connection = Websockets.WebSocketFactory.Create();
            connection.OnLog += Connection_OnLog;
            connection.OnError += Connection_OnError;
            connection.OnMessage += Connection_OnMessage;
            connection.OnOpened += Connection_OnOpened;

            //Timeout / Setup
            Echo = Failed = false;
            Timeout();

            //Do test

            Console.WriteLine("Connecting...");
            connection.Open("wss://echo.websocket.org");

            while (!connection.IsOpen && !Failed)
            {
                await Task.Delay(10);
            }

            if (!connection.IsOpen)
                return;
            Console.WriteLine("Connected !");

            System.Diagnostics.Trace.WriteLine("HI");
            Console.WriteLine("Sending...");
            connection.Send("Hello World");
            Console.WriteLine("Sent !");

            while (!Echo && !Failed)
            {
                await Task.Delay(10);
            }

            if (!Echo)
                return;

            Console.WriteLine("Received !");

            Console.WriteLine("Passed !");
            Trace.WriteLine("Passed");
            Assert.True(true);
        }

        private void Connection_OnOpened()
        {
            Debug.WriteLine("Opened !");
        }

        async void Timeout()
        {
            await Task.Delay(120000);
            Failed = true;
            Debug.WriteLine("Timeout");
        }

        private void Connection_OnMessage(string obj)
        {
            Echo = obj == "Hello World";
        }

        private void Connection_OnError(string obj)
        {
            Failed = true;
            Trace.Write("ERROR " + obj);
        }

        private void Connection_OnLog(string obj)
        {
            Trace.Write(obj);
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