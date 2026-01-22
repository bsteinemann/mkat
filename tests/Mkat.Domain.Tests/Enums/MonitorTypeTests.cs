using Mkat.Domain.Enums;

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
    public void MonitorType_HasExactlyThreeValues()
    {
        var values = Enum.GetValues<MonitorType>();
        Assert.Equal(3, values.Length);
    }
}
