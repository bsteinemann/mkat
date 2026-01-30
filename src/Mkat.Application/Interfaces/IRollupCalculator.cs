using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Application.Interfaces;

public interface IRollupCalculator
{
    MonitorRollup Compute(IReadOnlyList<MonitorEvent> events, Guid monitorId, Guid serviceId, Granularity granularity, DateTime periodStart);
}
