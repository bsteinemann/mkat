using Mkat.Domain.Enums;

namespace Mkat.Domain.Tests.Enums;

public class SeverityTests
{
    [Fact]
    public void Low_HasValue_Zero()
    {
        Assert.Equal(0, (int)Severity.Low);
    }

    [Fact]
    public void Medium_HasValue_One()
    {
        Assert.Equal(1, (int)Severity.Medium);
    }

    [Fact]
    public void High_HasValue_Two()
    {
        Assert.Equal(2, (int)Severity.High);
    }

    [Fact]
    public void Critical_HasValue_Three()
    {
        Assert.Equal(3, (int)Severity.Critical);
    }

    [Fact]
    public void Severity_HasExactlyFourValues()
    {
        var values = Enum.GetValues<Severity>();
        Assert.Equal(4, values.Length);
    }
}
