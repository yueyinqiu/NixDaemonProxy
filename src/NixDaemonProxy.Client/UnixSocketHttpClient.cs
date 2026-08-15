using System.Net.Sockets;

static class UnixSocketHttpClient
{
    public static HttpClient Create(string socket)
    {
        var httpHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (_, cancellationToken) =>
            {
                var s = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(socket);
                await s.ConnectAsync(endpoint, cancellationToken);
                return new NetworkStream(s, ownsSocket: true);
            }
        };
        return new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }
}