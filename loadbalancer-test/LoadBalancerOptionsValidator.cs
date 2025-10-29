using Microsoft.Extensions.Options;
using System.Net;

namespace loadbalancer_test;

public class LoadBalancerOptionsValidator : IValidateOptions<LoadBalancerOptions>
{
    public ValidateOptionsResult Validate(string? name, LoadBalancerOptions options)
    {
        var errors = new List<string>();

        if (options.Listener.Port <= 0 || options.Listener.Port > 65535)
            errors.Add("Listener.Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(options.Listener.Ip) || !IPAddress.TryParse(options.Listener.Ip, out _))
            errors.Add("Listener.Ip must be a valid IP address");

        foreach (var b in options.Backends)
        {
            if (b.Port <= 0 || b.Port > 65535)
                errors.Add($"Backend port {b.Port} must be between 1 and 65535");
            if (string.IsNullOrWhiteSpace(b.Host))
                errors.Add("Backend host must be specified");
        }

        if (options.Management.Port < 0 || options.Management.Port > 65535)
            errors.Add("Management.Port must be between 0 and 65535");
        if (string.IsNullOrWhiteSpace(options.Management.Ip) || !IPAddress.TryParse(options.Management.Ip, out _))
            errors.Add("Management.Ip must be a valid IP address");

        if (options.HealthCheck.IntervalSeconds <= 0)
            errors.Add("HealthCheck.IntervalSeconds must be > 0");
        if (options.HealthCheck.TimeoutSeconds <= 0)
            errors.Add("HealthCheck.TimeoutSeconds must be > 0");
        if (options.HealthCheck.FailureThreshold <= 0)
            errors.Add("HealthCheck.FailureThreshold must be > 0");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
