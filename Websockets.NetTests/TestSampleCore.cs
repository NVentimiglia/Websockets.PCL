using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Websockets.NetTests
{
    public class TestSampleCore
    {

        public async void DoTest()
        {
            var client = new ClientWebSocket();
            try
            {
                client.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
                client.ConnectAsync(new Uri(Program.WSECHOD_URL), CancellationToken.None).Wait();
                var data = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
                var bytebuf = new byte[1024];
                var buf = new ArraySegment<byte>(bytebuf);
                Receive(client);
                await client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("{0}: {1}", ex.ToString(), ex.StackTrace));
            }
        }

        public async Task Receive(ClientWebSocket client)
        {
            var data = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            var bytebuf = new byte[1024];
            var buf = new ArraySegment<byte>(bytebuf);
            var state = client.State;
            while (state == WebSocketState.Open)
            {
                Debug.WriteLine("receiving...");
                try
                {
                    var result = await client.ReceiveAsync(buf, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        //await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        Debug.WriteLine(string.Format("close: {0} ({1})", result.CloseStatus, result.CloseStatusDescription));
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary || result.MessageType == WebSocketMessageType.Text)
                    {
                        var compare = new byte[data.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            compare[i] = bytebuf[i];
                        }
                        if (compare.SequenceEqual(data))
                        {
                            Trace.WriteLine("passed");
                            //await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        }
                        else
                        {
                            Debug.WriteLine(string.Join("", compare));
                            Debug.WriteLine(Encoding.UTF8.GetString(compare));
                        }

                    }
                    state = client.State;
                }
                catch (Exception ex)
                {
                    state = client.State;
                    Trace.WriteLine(string.Format("{3}: {0}: last state ({2}): {1}", ex.Message, ex.StackTrace, state.ToString(), ex.GetType().ToString()));
                }
            }


        }
    }
}
