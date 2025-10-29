using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace loadbalancer_test.Domain.Abstractions;

public interface IHealthChecker
{
    Task<bool> CheckAsync(IPEndPoint endpoint, TimeSpan timeout, CancellationToken ct);
}
