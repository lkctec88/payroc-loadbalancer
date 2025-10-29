using loadbalancer_test;
using loadbalancer_test.Services;
using loadbalancer_test.Domain.Abstractions;
using loadbalancer_test.Infrastructure.Health;
using loadbalancer_test.Application.Routing;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LoadBalancerOptions>(builder.Configuration.GetSection("LoadBalancer"));

builder.Services.AddSingleton<IValidateOptions<LoadBalancerOptions>, LoadBalancerOptionsValidator>();

builder.Services.AddSingleton<IBackendPool, BackendPool>();
builder.Services.AddSingleton<IHealthChecker, TcpHealthChecker>();

builder.Services.AddSingleton<loadbalancer_test.Domain.Abstractions.IRoutingStrategy>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<LoadBalancerOptions>>().Value;
    switch (opt.RoutingStrategy)
    {
        case RoutingStrategyType.LeastConnections:
            return new LeastConnectionsRoutingStrategy();
        default:
            return new RoundRobinRoutingStrategy();
    }
});

builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddHostedService<LoadBalancerService>();
builder.Services.AddHostedService<ManagementService>();

var host = builder.Build();
host.Run();
