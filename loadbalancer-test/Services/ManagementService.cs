using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Services;

public class ManagementService : BackgroundService
{
    private readonly ILogger<ManagementService> _logger;
    private readonly IBackendPool _pool;
    private readonly IOptions<LoadBalancerOptions> _options;
    private TcpListener? _listener;

    public ManagementService(ILogger<ManagementService> logger, IBackendPool pool, IOptions<LoadBalancerOptions> options)
    {
        _logger = logger;
        _pool = pool;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var ip = IPAddress.Loopback;
            if (IPAddress.TryParse(_options.Value.Management.Ip, out var parsed)) ip = parsed;
            var port = _options.Value.Management.Port;
            _listener = new TcpListener(ip, port);
            _listener.Start();
            _logger.LogInformation("Management endpoint listening on {ip}:{port}", ip, port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Management endpoint error");
        }
        finally
        {
            try { _listener?.Stop(); } catch { }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var c = client;
        var stream = c.GetStream();
        var buffer = new byte[2048];
        try { await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct); } catch { }

        var snapshot = _pool.Snapshot();
        var overallHealthy = snapshot.Any(n => n.IsHealthy);
        var payload = new
        {
            overallHealthy,
            backends = snapshot.Select(n => new
            {
                endpoint = n.EndPoint.ToString(),
                healthy = n.IsHealthy,
                activeConnections = n.ActiveConnections
            }).ToArray()
        };
        var json = JsonSerializer.Serialize(payload);
        var statusCode = overallHealthy ? "200 OK" : "503 Service Unavailable";
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        var headers =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Content-Type: application/json\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headers);
        try
        {
            await stream.WriteAsync(headerBytes, ct);
            await stream.WriteAsync(bodyBytes, ct);
        }
        catch { }
    }
}
