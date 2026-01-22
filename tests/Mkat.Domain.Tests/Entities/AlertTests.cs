using Mkat.Domain.Entities;
using Mkat.Domain.Enums;

namespace Mkat.Domain.Tests.Entities;

public class AlertTests
{
    [Fact]
    public void NewAlert_HasEmptyMessage()
    {
        var alert = new Alert();
        Assert.Equal(string.Empty, alert.Message);
    }

    [Fact]
    public void NewAlert_HasNullAcknowledgedAt()
    {
        var alert = new Alert();
        Assert.Null(alert.AcknowledgedAt);
    }

    [Fact]
    public void NewAlert_HasNullDispatchedAt()
    {
        var alert = new Alert();
        Assert.Null(alert.DispatchedAt);
    }

    [Fact]
    public void NewAlert_HasNullMetadata()
    {
        var alert = new Alert();
        Assert.Null(alert.Metadata);
    }

    [Fact]
    public void NewAlert_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var alert = new Alert();
        var after = DateTime.UtcNow;

        Assert.InRange(alert.CreatedAt, before, after);
    }

    [Fact]
    public void Alert_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var acknowledgedAt = DateTime.UtcNow;
        var dispatchedAt = DateTime.UtcNow;

        var alert = new Alert
        {
            Id = id,
            ServiceId = serviceId,
            Type = AlertType.Failure,
            Severity = Severity.Critical,
            Message = "Service is down",
            AcknowledgedAt = acknowledgedAt,
            DispatchedAt = dispatchedAt,
            Metadata = "{\"reason\":\"timeout\"}"
        };

        Assert.Equal(id, alert.Id);
        Assert.Equal(serviceId, alert.ServiceId);
        Assert.Equal(AlertType.Failure, alert.Type);
        Assert.Equal(Severity.Critical, alert.Severity);
        Assert.Equal("Service is down", alert.Message);
        Assert.Equal(acknowledgedAt, alert.AcknowledgedAt);
        Assert.Equal(dispatchedAt, alert.DispatchedAt);
        Assert.Equal("{\"reason\":\"timeout\"}", alert.Metadata);
    }
}
