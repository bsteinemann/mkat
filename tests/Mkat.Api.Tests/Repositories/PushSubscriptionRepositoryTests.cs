using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Repositories;

public class PushSubscriptionRepositoryTests : IDisposable
{
    private readonly MkatDbContext _db;
    private readonly PushSubscriptionRepository _repo;

    public PushSubscriptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _db = new MkatDbContext(options);
        _repo = new PushSubscriptionRepository(_db);
    }

    [Fact]
    public async Task AddAsync_PersistsSubscription()
    {
        var sub = new PushSubscription
        {
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "key1",
            AuthKey = "auth1"
        };

        await _repo.AddAsync(sub);
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("https://push.example.com/sub1", all[0].Endpoint);
    }

    [Fact]
    public async Task RemoveByEndpointAsync_RemovesMatchingSubscription()
    {
        _db.Set<PushSubscription>().Add(new PushSubscription
        {
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "key1",
            AuthKey = "auth1"
        });
        await _db.SaveChangesAsync();

        await _repo.RemoveByEndpointAsync("https://push.example.com/sub1");
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSubscriptions()
    {
        _db.Set<PushSubscription>().AddRange(
            new PushSubscription { Endpoint = "https://a.com", P256dhKey = "k1", AuthKey = "a1" },
            new PushSubscription { Endpoint = "https://b.com", P256dhKey = "k2", AuthKey = "a2" }
        );
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    public void Dispose() => _db.Dispose();
}
