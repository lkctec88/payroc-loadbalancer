using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Services;

public class BackendPool : IBackendPool
{
    private readonly ILogger<BackendPool> _logger;
    private readonly List<BackendNode> _nodes = new();
    private readonly object _lock = new();

    public BackendPool(ILogger<BackendPool> logger, IOptions<LoadBalancerOptions> options)
    {
        _logger = logger;
        foreach (var b in options.Value.Backends)
        {
            _nodes.Add(new BackendNode(new IPEndPoint(Dns.GetHostAddresses(b.Host).First(), b.Port)));
        }
    }

    public IEnumerable<IBackendNode> Nodes
    {
        get
        {
            lock (_lock)
            {
                return _nodes.Cast<IBackendNode>().ToList();
            }
        }
    }

    public IReadOnlyList<IBackendNode> Snapshot()
    {
        lock (_lock)
        {
            return _nodes.Cast<IBackendNode>().ToList();
        }
    }

    public void MarkHealth(IPEndPoint endpoint, bool healthy)
    {
        lock (_lock)
        {
            var node = _nodes.FirstOrDefault(n => n.EndPoint.Equals(endpoint));
            if (node != null)
            {
                node.IsHealthy = healthy;
            }
        }
    }

    public class BackendNode : IBackendNode
    {
        private int _activeConnections = 0;

        public BackendNode(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        public IPEndPoint EndPoint { get; }
        public bool IsHealthy { get; set; } = true;
        public int ActiveConnections => Volatile.Read(ref _activeConnections);

        public void IncrementConnections() => Interlocked.Increment(ref _activeConnections);
        public void DecrementConnections()
        {
            var v = Interlocked.Decrement(ref _activeConnections);
            if (v < 0)
            {
                Interlocked.Exchange(ref _activeConnections, 0);
            }
        }
    }
}
