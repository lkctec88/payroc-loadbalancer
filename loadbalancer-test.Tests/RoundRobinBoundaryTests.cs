using System.Net;
using System.Reflection;
using loadbalancer_test.Application.Routing;
using loadbalancer_test.Domain.Abstractions;
using Xunit;

namespace loadbalancer_test.Tests;

public class RoundRobinBoundaryTests
{
    private static Services.BackendPool.BackendNode Node(int port, bool healthy = true)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);
        return new Services.BackendPool.BackendNode(ep) { IsHealthy = healthy };
    }

    [Fact]
    public void Select_ReturnsNull_WhenNoBackends()
    {
        IRoutingStrategy strat = new RoundRobinRoutingStrategy();
        var result = strat.Select(Array.Empty<IBackendNode>());
        Assert.Null(result);
    }

    [Fact]
    public void Select_ReturnsNull_WhenAllUnhealthy()
    {
        IRoutingStrategy strat = new RoundRobinRoutingStrategy();
        var nodes = new IBackendNode[] { Node(7001, false), Node(7002, false) };
        var result = strat.Select(nodes);
        Assert.Null(result);
    }

    [Fact]
    public void Select_SingleHealthy_ReturnsThatOne()
    {
        IRoutingStrategy strat = new RoundRobinRoutingStrategy();
        var nodes = new IBackendNode[] { Node(7001, false), Node(7002, true), Node(7003, false) };
        var result = strat.Select(nodes);
        Assert.NotNull(result);
        Assert.Equal(7002, result!.EndPoint.Port);
    }

    [Fact]
    public void Select_HandlesIntegerOverflow()
    {
        var strat = new RoundRobinRoutingStrategy();
        var field = typeof(RoundRobinRoutingStrategy).GetField("_next", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(strat, int.MaxValue - 1);

        var nodes = new IBackendNode[] { Node(7001), Node(7002), Node(7003) };
        for (int i = 0; i < 5; i++)
        {
            var sel = strat.Select(nodes);
            Assert.NotNull(sel);
            Assert.Contains(sel!, nodes);
        }
    }
}
