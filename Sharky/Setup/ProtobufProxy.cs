using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SC2APIProtocol;

namespace Sharky
{
    public class ProtobufProxy
    {
        private ClientWebSocket clientSocket;
        private CancellationToken token = new CancellationTokenSource().Token;

        public async Task Connect(string address, int port)
        {
            clientSocket = new ClientWebSocket();

            // Disable PING control frames (https://tools.ietf.org/html/rfc6455#section-5.5.2).
            // It seems SC2 built in websocket server does not do PONG but tries to process ping as
            // request and then sends empty response to client. 
            clientSocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);
            string adr = string.Format("ws://{0}:{1}/sc2api", address, port);
            Uri uri = new Uri(adr);
            await clientSocket.ConnectAsync(uri, token);

            await Ping();
        }

        public async Task Ping()
        {
            Request request = new Request();
            request.Ping = new RequestPing();
            Response response = await SendRequest(request);
        }

        public async Task<Response> SendRequest(Request request)
        {
            await WriteMessage(request);
            return await ReadMessage();
        }

        public async Task Quit()
        {
            Request quit = new Request();
            quit.Quit = new RequestQuit();
            await WriteMessage(quit);
        }

        private async Task WriteMessage(Request request)
        {
            byte[] sendBuf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            CodedOutputStream outStream = new CodedOutputStream(sendBuf);
            request.WriteTo(outStream);
            await clientSocket.SendAsync(new ArraySegment<byte>(sendBuf, 0, (int)outStream.Position), WebSocketMessageType.Binary, true, token);
            ArrayPool<byte>.Shared.Return(sendBuf);
        }

        private async Task<Response> ReadMessage()
        {
            byte[] receiveBuf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            bool finished = false;
            int curPos = 0;
            while (!finished)
            {
                int left = receiveBuf.Length - curPos;
                if (left < 0)
                {
                    // No space left in the array, enlarge the array by doubling its size.
                    byte[] temp = new byte[receiveBuf.Length * 2];
                    Array.Copy(receiveBuf, temp, receiveBuf.Length);
                    ArrayPool<byte>.Shared.Return(receiveBuf);
                    receiveBuf = temp;
                    left = receiveBuf.Length - curPos;
                }
                WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuf, curPos, left), token);
                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    throw new Exception("Expected Binary message type.");
                }

                curPos += result.Count;
                finished = result.EndOfMessage;
            }

            Response response = Response.Parser.ParseFrom(new System.IO.MemoryStream(receiveBuf, 0, curPos));
            ArrayPool<byte>.Shared.Return(receiveBuf);

            return response;
        }
    }
}
