using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class PeerTests
{
    [Fact]
    public void Peer_HasRequiredProperties()
    {
        var peer = new Peer
        {
            Id = Guid.NewGuid(),
            Name = "Test Peer",
            Url = "https://peer.example.com",
            HeartbeatToken = "hb-token-123",
            WebhookToken = "wh-token-456",
            ServiceId = Guid.NewGuid(),
            PairedAt = DateTime.UtcNow,
            HeartbeatIntervalSeconds = 30
        };

        Assert.NotEqual(Guid.Empty, peer.Id);
        Assert.Equal("Test Peer", peer.Name);
        Assert.Equal("https://peer.example.com", peer.Url);
        Assert.Equal("hb-token-123", peer.HeartbeatToken);
        Assert.Equal("wh-token-456", peer.WebhookToken);
        Assert.NotEqual(Guid.Empty, peer.ServiceId);
        Assert.Equal(30, peer.HeartbeatIntervalSeconds);
    }

    [Fact]
    public void Peer_DefaultHeartbeatInterval_Is30()
    {
        var peer = new Peer();
        Assert.Equal(30, peer.HeartbeatIntervalSeconds);
    }

    [Fact]
    public void Peer_DefaultName_IsEmpty()
    {
        var peer = new Peer();
        Assert.Equal(string.Empty, peer.Name);
    }

    [Fact]
    public void Peer_DefaultUrl_IsEmpty()
    {
        var peer = new Peer();
        Assert.Equal(string.Empty, peer.Url);
    }

    [Fact]
    public void Peer_DefaultTokens_AreEmpty()
    {
        var peer = new Peer();
        Assert.Equal(string.Empty, peer.HeartbeatToken);
        Assert.Equal(string.Empty, peer.WebhookToken);
    }

    [Fact]
    public void Peer_ServiceNavigation_IsNull()
    {
        var peer = new Peer();
        Assert.Null(peer.Service);
    }
}
