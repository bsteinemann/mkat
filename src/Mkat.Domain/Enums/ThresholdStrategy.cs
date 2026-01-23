namespace Mkat.Domain.Enums;

public enum ThresholdStrategy
{
    Immediate = 0,
    ConsecutiveCount = 1,
    TimeDurationAverage = 2,
    SampleCountAverage = 3
}
