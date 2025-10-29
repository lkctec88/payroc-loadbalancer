using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Services;

public class LoadBalancerService : BackgroundService
{
    private readonly ILogger<LoadBalancerService> _logger;
    private readonly IBackendPool _pool;
    private readonly IOptions<LoadBalancerOptions> _options;
    private readonly IRoutingStrategy _routing;

    private Socket? _listener;

    public LoadBalancerService(ILogger<LoadBalancerService> logger, IBackendPool pool, IOptions<LoadBalancerOptions> options, IRoutingStrategy routing)
    {
        _logger = logger;
        _pool = pool;
        _options = options;
        _routing = routing;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listenIp = IPAddress.Any;
        if (IPAddress.TryParse(_options.Value.Listener.Ip, out var ip)) listenIp = ip;
        var localEndpoint = new IPEndPoint(listenIp, _options.Value.Listener.Port);

        _listener = new Socket(localEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };
        _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Bind(localEndpoint);
        _listener.Listen(backlog: 512);

        _logger.LogInformation("Load balancer listening on {endpoint} using {strategy}", localEndpoint, _options.Value.RoutingStrategy);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptAsync(stoppingToken);
                _ =HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Listener error");
        }
        finally
        {
            try { _listener?.Close(); } catch { }
        }
    }

    private async Task HandleClientAsync(Socket client, CancellationToken ct)
    {
        _logger.LogInformation("HandleClientAsync start {client}", client.RemoteEndPoint);
        Socket? backend = null;
        IBackendNode? node = null;
        try
        {
            node = _routing.Select(_pool.Snapshot());
            if (node == null)
            {
                _logger.LogWarning("No healthy backends available; rejecting client");
                client.Close();
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                try
                {
                    var clientEp = client.RemoteEndPoint?.ToString() ?? "?";
                    _logger.LogDebug("Routing client {client} -> backend {backend}", clientEp, node.EndPoint);
                }
                catch { }
            }

            node.IncrementConnections();
            backend = new Socket(node.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromSeconds(_options.Value.HealthCheck.TimeoutSeconds));
            await backend.ConnectAsync(node.EndPoint, connectCts.Token);

            var pump1 = PumpAsync(client, backend, ct); // client->backend
            var pump2 = PumpAsync(backend, client, ct); // backend->client
            await Task.WhenAny(pump1, pump2);
            // Half-close and wait briefly for the other direction to flush
            try { client.Shutdown(SocketShutdown.Send); } catch { }
            try { backend.Shutdown(SocketShutdown.Send); } catch { }
            await Task.WhenAll(Task.WhenAny(pump1, Task.Delay(100, ct)), Task.WhenAny(pump2, Task.Delay(100, ct)));
        }
        catch (OperationCanceledException)
        {
            // ignore can log if needed
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling client; closing connection");
        }
        finally
        {
            try { backend?.Close(); } catch { }
            try { client.Close(); } catch { }
            if (node != null) node.DecrementConnections();
        }
    }

    private static async Task PumpAsync(Socket source, Socket dest, CancellationToken ct)
    {
        var buffer = new byte[32 * 1024];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int read = await source.ReceiveAsync(buffer, SocketFlags.None, ct);
                if (read <= 0) break;
                int sent = 0;
                while (sent < read)
                {
                    sent += await dest.SendAsync(new ArraySegment<byte>(buffer, sent, read - sent), SocketFlags.None, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
    }
}
