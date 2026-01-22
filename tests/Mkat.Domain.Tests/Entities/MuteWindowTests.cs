using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class MuteWindowTests
{
    [Fact]
    public void NewMuteWindow_HasNullReason()
    {
        var muteWindow = new MuteWindow();
        Assert.Null(muteWindow.Reason);
    }

    [Fact]
    public void NewMuteWindow_HasCreatedAt_SetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var muteWindow = new MuteWindow();
        var after = DateTime.UtcNow;

        Assert.InRange(muteWindow.CreatedAt, before, after);
    }

    [Fact]
    public void NewMuteWindow_HasDefaultStartsAt()
    {
        var muteWindow = new MuteWindow();
        Assert.Equal(default, muteWindow.StartsAt);
    }

    [Fact]
    public void NewMuteWindow_HasDefaultEndsAt()
    {
        var muteWindow = new MuteWindow();
        Assert.Equal(default, muteWindow.EndsAt);
    }

    [Fact]
    public void MuteWindow_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var startsAt = DateTime.UtcNow;
        var endsAt = startsAt.AddHours(2);

        var muteWindow = new MuteWindow
        {
            Id = id,
            ServiceId = serviceId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = "Scheduled maintenance"
        };

        Assert.Equal(id, muteWindow.Id);
        Assert.Equal(serviceId, muteWindow.ServiceId);
        Assert.Equal(startsAt, muteWindow.StartsAt);
        Assert.Equal(endsAt, muteWindow.EndsAt);
        Assert.Equal("Scheduled maintenance", muteWindow.Reason);
    }
}
