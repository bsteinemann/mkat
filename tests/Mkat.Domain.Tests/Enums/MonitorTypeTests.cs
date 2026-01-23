using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Enums;

public class MonitorTypeTests
{
    [Fact]
    public void Webhook_HasValue_Zero()
    {
        Assert.Equal(0, (int)MonitorType.Webhook);
    }

    [Fact]
    public void Heartbeat_HasValue_One()
    {
        Assert.Equal(1, (int)MonitorType.Heartbeat);
    }

    [Fact]
    public void HealthCheck_HasValue_Two()
    {
        Assert.Equal(2, (int)MonitorType.HealthCheck);
    }

    [Fact]
    public void Metric_HasValue_Three()
    {
        Assert.Equal(3, (int)MonitorType.Metric);
    }

    [Fact]
    public void MonitorType_HasExactlyFourValues()
    {
        var values = Enum.GetValues<MonitorType>();
        Assert.Equal(4, values.Length);
    }

}
