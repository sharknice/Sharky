namespace Sharky
{
    public class FastProtobufProxy
    {
        ClientWebSocket ClientWebSocket;

        public FastProtobufProxy()
        {
            ClientWebSocket = new ClientWebSocket();
            ClientWebSocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);
        }

        public async Task Connect(string address, int port)
        {
            var connection = string.Format("ws://{0}:{1}/sc2api", address, port);
            await ClientWebSocket.ConnectAsync(new Uri(connection), CancellationToken.None);

            await SendRequest(new Request { Ping = new RequestPing() });
        }

        async Task Send(Request request)
        {
            var byteArray = request.ToByteArray();
            var data = new ArraySegment<byte>(byteArray);
            await ClientWebSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public async Task<Response> SendRequest(Request request)
        {        
            await Send(request);
            return await Receive();
        }

        async Task<Response> Receive()
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            var ms = new MemoryStream();

            var result = await ClientWebSocket.ReceiveAsync(buffer, CancellationToken.None);

            ms.Write(buffer.Array, buffer.Offset, result.Count);

            var count = 0;
            while (!result.EndOfMessage)
            {
                result = await ClientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer.Array, buffer.Offset, result.Count);

                count++;
            }

            ms.Seek(0, SeekOrigin.Begin);
            var response = Response.Parser.ParseFrom(ms);

            return response;
        }
    }
}
