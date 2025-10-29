using System.Net;
using loadbalancer_test;
using loadbalancer_test.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace loadbalancer_test.Tests;

public class BackendPoolBoundaryTests
{
    private static BackendPool CreatePool(params int[] ports)
    {
        var options = Options.Create(new LoadBalancerOptions
        {
            Backends = ports.Select(p => new BackendEndpoint{ Host = "127.0.0.1", Port = p }).ToList()
        });
        return new BackendPool(NullLogger<BackendPool>.Instance, options);
    }

    [Fact]
    public void MarkHealth_NonExistentEndpoint_NoChange()
    {
        var pool = CreatePool(7001, 7002);
        var snapshot = pool.Snapshot();
        var before = snapshot.Select(n => (n.EndPoint, n.IsHealthy)).ToList();

        var nonexistent = new IPEndPoint(IPAddress.Loopback, 9999);
        pool.MarkHealth(nonexistent, false);

        var after = pool.Snapshot().Select(n => (n.EndPoint, n.IsHealthy)).ToList();
        Assert.Equal(before, after);
    }

    [Fact]
    public void Snapshot_EmptyList_DoesNotThrow()
    {
        var options = Options.Create(new LoadBalancerOptions { Backends = new List<BackendEndpoint>() });
        var pool = new BackendPool(NullLogger<BackendPool>.Instance, options);
        var snap = pool.Snapshot();
        Assert.NotNull(snap);
        Assert.Empty(snap);
    }
}
