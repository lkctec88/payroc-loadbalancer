using System.Net;
using System.Net.Sockets;
using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Infrastructure.Health;

public class TcpHealthChecker : IHealthChecker
{
    public async Task<bool> CheckAsync(IPEndPoint endpoint, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            await socket.ConnectAsync(endpoint, cts.Token);
            return socket.Connected;
        }
        catch
        {
            return false;
        }
    }
}
