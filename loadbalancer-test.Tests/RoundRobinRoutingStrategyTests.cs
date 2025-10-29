using System.Net;
using loadbalancer_test.Application.Routing;
using loadbalancer_test.Domain.Abstractions;
using Xunit;

namespace loadbalancer_test.Tests;

public class RoundRobinRoutingStrategyTests
{
    private static Services.BackendPool.BackendNode Node(int port, bool healthy = true)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);
        var n = new Services.BackendPool.BackendNode(ep) { IsHealthy = healthy };
        return n;
    }

    [Fact]
    public void Returns_Healthy_Nodes_In_Order()
    {
        IBackendNode[] nodes = new IBackendNode[] { Node(7001), Node(7002), Node(7003) };
        IRoutingStrategy strat = new RoundRobinRoutingStrategy();

        var s1 = strat.Select(nodes);
        var s2 = strat.Select(nodes);
        var s3 = strat.Select(nodes);
        var s4 = strat.Select(nodes);
        var s5 = strat.Select(nodes);

        Assert.Equal(7001, s1!.EndPoint.Port);
        Assert.Equal(7002, s2!.EndPoint.Port);
        Assert.Equal(7003, s3!.EndPoint.Port);
        Assert.Equal(7001, s4!.EndPoint.Port);
        Assert.Equal(7002, s5!.EndPoint.Port);
    }

    [Fact]
    public void Skips_Unhealthy_Nodes()
    {
        IBackendNode[] nodes = new IBackendNode[] { Node(7001), Node(7002, healthy: false), Node(7003) };
        IRoutingStrategy strat = new RoundRobinRoutingStrategy();

        var s1 = strat.Select(nodes);
        var s2 = strat.Select(nodes);
        var s3 = strat.Select(nodes);
        var s4 = strat.Select(nodes);

        Assert.Equal(7001, s1!.EndPoint.Port);
        Assert.Equal(7003, s2!.EndPoint.Port);
        Assert.Equal(7001, s3!.EndPoint.Port);
        Assert.Equal(7003, s4!.EndPoint.Port);
    }
}
