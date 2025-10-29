using System.Net;

namespace loadbalancer_test.Domain.Abstractions;

public interface IBackendPool
{
    IReadOnlyList<IBackendNode> Snapshot();
    IEnumerable<IBackendNode> Nodes { get; }
    void MarkHealth(IPEndPoint endpoint, bool healthy);
}

public interface IBackendNode
{
    IPEndPoint EndPoint { get; }
    bool IsHealthy { get; }
    int ActiveConnections { get; }
    void IncrementConnections();
    void DecrementConnections();
}
