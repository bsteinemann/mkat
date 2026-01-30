using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Enums;

public class AlertTypeTests
{
    [Fact]
    public void Failure_HasValue_Zero()
    {
        Assert.Equal(0, (int)AlertType.Failure);
    }

    [Fact]
    public void Recovery_HasValue_One()
    {
        Assert.Equal(1, (int)AlertType.Recovery);
    }

    [Fact]
    public void MissedHeartbeat_HasValue_Two()
    {
        Assert.Equal(2, (int)AlertType.MissedHeartbeat);
    }

    [Fact]
    public void FailedHealthCheck_HasValue_Three()
    {
        Assert.Equal(3, (int)AlertType.FailedHealthCheck);
    }

    [Fact]
    public void AlertType_HasExactlyFourValues()
    {
        var values = Enum.GetValues<AlertType>();
        Assert.Equal(4, values.Length);
    }
}
