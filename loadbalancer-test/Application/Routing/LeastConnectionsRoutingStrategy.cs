using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Application.Routing;

public class LeastConnectionsRoutingStrategy : IRoutingStrategy
{
    public IBackendNode? Select(IReadOnlyList<IBackendNode> nodes)
    {
        IBackendNode? best = null;
        foreach (var n in nodes)
        {
            if (!n.IsHealthy) continue;
            if (best == null || n.ActiveConnections < best.ActiveConnections)
                best = n;
        }
        return best;
    }
}
