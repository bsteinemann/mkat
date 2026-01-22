using Mkat.Domain.Enums;

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
    public void AlertType_HasExactlyThreeValues()
    {
        var values = Enum.GetValues<AlertType>();
        Assert.Equal(3, values.Length);
    }
}
