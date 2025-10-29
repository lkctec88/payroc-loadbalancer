using System.Net;
using System.Net.Sockets;
using loadbalancer_test;
using loadbalancer_test.Services;
using loadbalancer_test.Domain.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace loadbalancer_test.Tests;

public class HealthCheckBoundaryTests
{
    private sealed class FakeChecker : IHealthChecker
    {
        private readonly bool _result;
        public FakeChecker(bool result) => _result = result;
        public Task<bool> CheckAsync(IPEndPoint endpoint, TimeSpan timeout, CancellationToken ct) => Task.FromResult(_result);
    }

    private static HealthCheckService CreateService(int intervalSeconds, int timeoutSeconds, int failureThreshold, params (string host, int port)[] backends)
    {
        var options = Options.Create(new LoadBalancerOptions
        {
            Backends = backends.Select(b => new BackendEndpoint{ Host = b.host, Port = b.port }).ToList(),
            HealthCheck = new LoadBalancerOptions.HealthCheckOptions
            {
                IntervalSeconds = intervalSeconds,
                TimeoutSeconds = timeoutSeconds,
                FailureThreshold = failureThreshold
            },
            Management = new LoadBalancerOptions.ManagementOptions { Ip = "127.0.0.1", Port = 0, SummaryIntervalSeconds = 3600 }
        });
        var pool = new BackendPool(NullLogger<BackendPool>.Instance, options);
        var checker = new FakeChecker(result: false); // always fail
        return new HealthCheckService(NullLogger<HealthCheckService>.Instance, pool, options, checker);
    }

    [Fact]
    public async Task MarksUnhealthy_WhenFailureThresholdIsOne()
    {
        int port = GetUnusedPort();
        var svc = CreateService(intervalSeconds: 1, timeoutSeconds: 1, failureThreshold: 1, ("127.0.0.1", port));
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await svc.StartAsync(cts.Token);
        try
        {
            await Task.Delay(1500, cts.Token);
            var poolField = typeof(HealthCheckService).GetField("_pool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pool = (IBackendPool)poolField!.GetValue(svc)!;
            Assert.All(pool.Snapshot(), n => Assert.False(n.IsHealthy));
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    private static int GetUnusedPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port; // immediately unused
    }
}
