using Mkat.Domain.Enums;
using Xunit;

namespace Mkat.Domain.Tests.Enums;

public class ThresholdStrategyTests
{
    [Fact]
    public void Immediate_HasValue_Zero()
    {
        Assert.Equal(0, (int)ThresholdStrategy.Immediate);
    }

    [Fact]
    public void ConsecutiveCount_HasValue_One()
    {
        Assert.Equal(1, (int)ThresholdStrategy.ConsecutiveCount);
    }

    [Fact]
    public void TimeDurationAverage_HasValue_Two()
    {
        Assert.Equal(2, (int)ThresholdStrategy.TimeDurationAverage);
    }

    [Fact]
    public void SampleCountAverage_HasValue_Three()
    {
        Assert.Equal(3, (int)ThresholdStrategy.SampleCountAverage);
    }

    [Fact]
    public void ThresholdStrategy_HasExactlyFourValues()
    {
        var values = Enum.GetValues<ThresholdStrategy>();
        Assert.Equal(4, values.Length);
    }
}
