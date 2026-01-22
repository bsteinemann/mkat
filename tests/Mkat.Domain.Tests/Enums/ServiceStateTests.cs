using Mkat.Domain.Enums;

namespace Mkat.Domain.Tests.Enums;

public class ServiceStateTests
{
    [Fact]
    public void Unknown_HasValue_Zero()
    {
        Assert.Equal(0, (int)ServiceState.Unknown);
    }

    [Fact]
    public void Up_HasValue_One()
    {
        Assert.Equal(1, (int)ServiceState.Up);
    }

    [Fact]
    public void Down_HasValue_Two()
    {
        Assert.Equal(2, (int)ServiceState.Down);
    }

    [Fact]
    public void Paused_HasValue_Three()
    {
        Assert.Equal(3, (int)ServiceState.Paused);
    }

    [Fact]
    public void ServiceState_HasExactlyFourValues()
    {
        var values = Enum.GetValues<ServiceState>();
        Assert.Equal(4, values.Length);
    }
}
