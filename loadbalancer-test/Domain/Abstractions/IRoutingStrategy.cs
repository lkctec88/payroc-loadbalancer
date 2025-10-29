namespace loadbalancer_test.Domain.Abstractions;

public interface IRoutingStrategy
{
    IBackendNode? Select(IReadOnlyList<IBackendNode> nodes);
}
