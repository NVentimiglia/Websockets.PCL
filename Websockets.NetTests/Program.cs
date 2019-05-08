using System;
using System.Threading.Tasks;

namespace Websockets.NetTests
{
    class Program
    {
        public static readonly string WSECHOD_URL = "wss://wsecho.n4v.eu";
        //public static readonly string WSECHOD_URL = "wss://localhost:8081";

        static void Main(string[] args)
        {
            Net.WebsocketConnection.Link();

            var testcore = new TestSampleCore();
            testcore.DoTest();

            var test = new TestSample();
            test.Setup();

            var test2 = new TestSampleData();
            test2.Setup();

            Task.WaitAll(new Task[] { test.DoTest(), test2.DoTest() });
        }
    }
}
