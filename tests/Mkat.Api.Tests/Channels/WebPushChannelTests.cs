using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Channels;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Channels;

public class WebPushChannelTests : IDisposable
{
    private readonly MkatDbContext _db;
    private readonly PushSubscriptionRepository _repo;
    private readonly Mock<ILogger<WebPushChannel>> _logger;

    public WebPushChannelTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _db = new MkatDbContext(options);
        _repo = new PushSubscriptionRepository(_db);
        _logger = new Mock<ILogger<WebPushChannel>>();
    }

    [Fact]
    public void ChannelType_ReturnsWebPush()
    {
        var vapidOptions = Options.Create(new VapidOptions());
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.Equal("WebPush", channel.ChannelType);
    }

    [Fact]
    public void IsEnabled_TrueWhenVapidConfigured()
    {
        var vapidOptions = Options.Create(new VapidOptions
        {
            PublicKey = "BNcRdFakePublicKey",
            PrivateKey = "fakePrivateKey",
            Subject = "mailto:admin@example.com"
        });
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public void IsEnabled_FalseWhenVapidNotConfigured()
    {
        var vapidOptions = Options.Create(new VapidOptions());
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.False(channel.IsEnabled);
    }

    [Fact]
    public async Task SendAlertAsync_ReturnsTrueWhenNoSubscriptions()
    {
        var vapidOptions = Options.Create(new VapidOptions
        {
            PublicKey = "BNcRdFakePublicKey",
            PrivateKey = "fakePrivateKey",
            Subject = "mailto:admin@example.com"
        });
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Message = "Service is down",
            Severity = Severity.Critical,
            Type = AlertType.Failure
        };
        var service = new Service { Id = alert.ServiceId, Name = "TestService" };

        var result = await channel.SendAlertAsync(alert, service);
        Assert.True(result);
    }

    [Fact]
    public async Task SendAlertAsync_ReturnsFalseWhenNotEnabled()
    {
        var vapidOptions = Options.Create(new VapidOptions());
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Message = "Service is down",
            Severity = Severity.Critical,
            Type = AlertType.Failure
        };
        var service = new Service { Id = alert.ServiceId, Name = "TestService" };

        var result = await channel.SendAlertAsync(alert, service);
        Assert.False(result);
    }

    public void Dispose() => _db.Dispose();
}
