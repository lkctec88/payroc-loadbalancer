using System.Net;
using Microsoft.Extensions.Options;
using loadbalancer_test.Domain.Abstractions;

namespace loadbalancer_test.Services;

public class HealthCheckService : BackgroundService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IBackendPool _pool;
    private readonly IOptions<LoadBalancerOptions> _options;
    private readonly IHealthChecker _checker;

    private readonly Dictionary<IPEndPoint, int> _failures = new();

    public HealthCheckService(ILogger<HealthCheckService> logger, IBackendPool pool, IOptions<LoadBalancerOptions> options, IHealthChecker checker)
    {
        _logger = logger;
        _pool = pool;
        _options = options;
        _checker = checker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.Value.HealthCheck.IntervalSeconds);
        var timeout = TimeSpan.FromSeconds(_options.Value.HealthCheck.TimeoutSeconds);
        var threshold = _options.Value.HealthCheck.FailureThreshold;
        var summaryEvery = TimeSpan.FromSeconds(_options.Value.Management.SummaryIntervalSeconds);
        var lastSummary = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var node in _pool.Nodes)
            {
                bool ok = await _checker.CheckAsync(node.EndPoint, timeout, stoppingToken);
                if (!ok)
                {
                    _failures.TryGetValue(node.EndPoint, out int count);
                    count++;
                    _failures[node.EndPoint] = count;
                    if (count >= threshold && node.IsHealthy)
                    {
                        _logger.LogWarning("Marking backend {backend} as UNHEALTHY", node.EndPoint);
                        _pool.MarkHealth(node.EndPoint, false);
                    }
                }
                else
                {
                    _failures[node.EndPoint] = 0;
                    if (!node.IsHealthy)
                    {
                        _logger.LogInformation("Marking backend {backend} as HEALTHY", node.EndPoint);
                        _pool.MarkHealth(node.EndPoint, true);
                    }
                }
            }

            // Periodic summary log
            if (DateTimeOffset.UtcNow - lastSummary >= summaryEvery)
            {
                lastSummary = DateTimeOffset.UtcNow;
                var snapshot = _pool.Snapshot();
                var healthy = snapshot.Where(n => n.IsHealthy).ToList();
                var unhealthy = snapshot.Where(n => !n.IsHealthy).ToList();
                _logger.LogInformation("Backend health: {healthyCount}/{total} healthy. Healthy: [{healthy}] Unhealthy: [{unhealthy}]",
                    healthy.Count, snapshot.Count,
                    string.Join(", ", healthy.Select(n => $"{n.EndPoint} (conns={n.ActiveConnections})")),
                    string.Join(", ", unhealthy.Select(n => n.EndPoint.ToString())));
            }

            try { await Task.Delay(interval, stoppingToken); } catch { }
        }
    }
}
