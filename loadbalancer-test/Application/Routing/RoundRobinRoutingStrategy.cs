using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Application.Routing;

public class RoundRobinRoutingStrategy : IRoutingStrategy
{
    private int _next = 0;
    public IBackendNode? Select(IReadOnlyList<IBackendNode> nodes)
    {
        if (nodes.Count == 0) return null;
        for (int i = 0; i < nodes.Count; i++)
        {
            var value = Interlocked.Increment(ref _next) - 1;
            var idx = (int)((uint)value % (uint)nodes.Count);
            var node = nodes[idx];
            if (node.IsHealthy) return node;
        }
        return null;
    }
}
