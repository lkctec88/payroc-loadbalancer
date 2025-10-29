namespace loadbalancer_test;

public class LoadBalancerOptions
{
    public ListenerOptions Listener { get; set; } = new();
    public List<BackendEndpoint> Backends { get; set; } = new();
    public HealthCheckOptions HealthCheck { get; set; } = new();
    public RoutingStrategyType RoutingStrategy { get; set; } = RoutingStrategyType.RoundRobin;
    public ManagementOptions Management { get; set; } = new();

    public class ListenerOptions
    {
        public string Ip { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 8080;
    }

    public class HealthCheckOptions
    {
        public int IntervalSeconds { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 2;
        public int FailureThreshold { get; set; } = 2;
    }

    public class ManagementOptions
    {
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9100;
        public int SummaryIntervalSeconds { get; set; } = 30;
    }
}

public class BackendEndpoint
{
    public string Host { get; set; } = "";
    public int Port { get; set; }

    public override string ToString() => $"{Host}:{Port}";
}

public enum RoutingStrategyType
{
    RoundRobin,
    LeastConnections
}
