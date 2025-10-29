using System.Net;
using loadbalancer_test.Services;
using Xunit;

namespace loadbalancer_test.Tests;

public class BackendPoolTests
{
    [Fact]
    public void MarkHealth_Changes_Node_State()
    {
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<BackendPool>();
        var options = Microsoft.Extensions.Options.Options.Create(new LoadBalancerOptions
        {
            Backends = new List<BackendEndpoint>
            {
                new BackendEndpoint{ Host = "127.0.0.1", Port = 7001 },
                new BackendEndpoint{ Host = "127.0.0.1", Port = 7002 }
            }
        });

        var pool = new BackendPool(logger, options);
        var node = pool.Snapshot().First();

        Assert.True(node.IsHealthy);
        pool.MarkHealth(node.EndPoint, false);
        Assert.False(pool.Snapshot().First(n => n.EndPoint.Equals(node.EndPoint)).IsHealthy);
        pool.MarkHealth(node.EndPoint, true);
        Assert.True(pool.Snapshot().First(n => n.EndPoint.Equals(node.EndPoint)).IsHealthy);
    }
}
