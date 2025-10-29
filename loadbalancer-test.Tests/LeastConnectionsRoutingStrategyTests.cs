using System.Net;
using loadbalancer_test.Application.Routing;
using loadbalancer_test.Domain.Abstractions;
using Xunit;

namespace loadbalancer_test.Tests;

public class LeastConnectionsRoutingStrategyTests
{
    private static Services.BackendPool.BackendNode Node(int port, int connections, bool healthy = true)
    {
        var ep = new IPEndPoint(IPAddress.Loopback, port);
        var n = new Services.BackendPool.BackendNode(ep) { IsHealthy = healthy };
        for (int i = 0; i < connections; i++) n.IncrementConnections();
        return n;
    }

    [Fact]
    public void Picks_Node_With_Fewest_Connections()
    {
        IRoutingStrategy strat = new LeastConnectionsRoutingStrategy();
        IBackendNode[] nodes = new IBackendNode[] { Node(7001, 3), Node(7002, 1), Node(7003, 2) };

        var selected = strat.Select(nodes);

        Assert.Equal(7002, selected!.EndPoint.Port);
    }

    [Fact]
    public void Skips_Unhealthy_Even_If_Fewer_Connections()
    {
        IRoutingStrategy strat = new LeastConnectionsRoutingStrategy();
        IBackendNode[] nodes = new IBackendNode[] { Node(7001, 3), Node(7002, 0, healthy: false), Node(7003, 2) };

        var selected = strat.Select(nodes);

        Assert.Equal(7003, selected!.EndPoint.Port);
    }
}
