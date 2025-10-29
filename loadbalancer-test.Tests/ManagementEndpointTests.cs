using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using loadbalancer_test;
using loadbalancer_test.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace loadbalancer_test.Tests;

public class ManagementEndpointTests
{
    private static int GetFreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static async Task WaitForListeningAsync(int port, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var client = new TcpClient();
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
                await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
                return;
            }
            catch
            {
                await Task.Delay(50);
            }
        }
        throw new TimeoutException($"Port {port} did not open in time");
    }

    private static ManagementService CreateManagementService(int mgmtPort, List<BackendEndpoint>? backends = null)
    {
        var options = Options.Create(new LoadBalancerOptions
        {
            Backends = backends ?? new List<BackendEndpoint>(),
            Management = new LoadBalancerOptions.ManagementOptions
            {
                Ip = "127.0.0.1",
                Port = mgmtPort,
                SummaryIntervalSeconds = 1
            },
            HealthCheck = new LoadBalancerOptions.HealthCheckOptions
            {
                IntervalSeconds = 100,
                TimeoutSeconds = 1,
                FailureThreshold = 100
            }
        });
        var pool = new BackendPool(NullLogger<BackendPool>.Instance, options);
        return new ManagementService(NullLogger<ManagementService>.Instance, pool, options);
    }

    [Fact]
    public async Task ManagementEndpoint_Returns503_WhenNoBackends()
    {
        int port = GetFreeTcpPort();
        var svc = CreateManagementService(port);
        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitForListeningAsync(port, TimeSpan.FromSeconds(2));
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var resp = await http.GetAsync($"http://127.0.0.1:{port}/");
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, resp.StatusCode);
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.GetProperty("overallHealthy").GetBoolean());
            Assert.Equal(0, doc.RootElement.GetProperty("backends").GetArrayLength());
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ManagementEndpoint_Returns200_WhenAtLeastOneBackendConfigured()
    {
        int port = GetFreeTcpPort();
        var backends = new List<BackendEndpoint>
        {
            new BackendEndpoint{ Host = "127.0.0.1", Port = 65000 }
        };
        var svc = CreateManagementService(port, backends);
        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitForListeningAsync(port, TimeSpan.FromSeconds(2));
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var resp = await http.GetAsync($"http://127.0.0.1:{port}/");
            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("overallHealthy").GetBoolean());
            Assert.Equal(1, doc.RootElement.GetProperty("backends").GetArrayLength());
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }
}
