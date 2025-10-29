using System.Net;
using loadbalancer_test.Application.Routing;
using loadbalancer_test.Domain.Abstractions;
using Xunit;

namespace loadbalancer_test.Tests;

public class LeastConnectionsBoundaryTests
{
    private static Services.BackendPool.BackendNode Node(int port, int conns, bool healthy = true)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);
        var n = new Services.BackendPool.BackendNode(ep) { IsHealthy = healthy };
        for (int i = 0; i < conns; i++) n.IncrementConnections();
        return n;
    }

    [Fact]
    public void Select_ReturnsNull_WhenNoBackends()
    {
        IRoutingStrategy strat = new LeastConnectionsRoutingStrategy();
        var result = strat.Select(Array.Empty<IBackendNode>());
        Assert.Null(result);
    }

    [Fact]
    public void Select_ReturnsNull_WhenAllUnhealthy()
    {
        IRoutingStrategy strat = new LeastConnectionsRoutingStrategy();
        IBackendNode[] nodes = new IBackendNode[] { Node(7001, 0, false), Node(7002, 1, false) };
        var result = strat.Select(nodes);
        Assert.Null(result);
    }

    [Fact]
    public void Select_TieOnConnections_PicksFirstHealthy()
    {
        IRoutingStrategy strat = new LeastConnectionsRoutingStrategy();
        IBackendNode[] nodes = new IBackendNode[] { Node(7001, 1), Node(7002, 1), Node(7003, 2) };
        var result = strat.Select(nodes);
        Assert.Equal(7001, result!.EndPoint.Port);
    }
}
