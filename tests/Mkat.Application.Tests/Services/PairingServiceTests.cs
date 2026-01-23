using System.Text;
using System.Text.Json;
using Mkat.Application.Services;
using Xunit;

namespace Mkat.Application.Tests.Services;

public class PairingServiceTests
{
    private readonly PairingService _service;

    public PairingServiceTests()
    {
        _service = new PairingService();
    }

    [Fact]
    public void GenerateToken_ReturnsBase64String()
    {
        var token = _service.GenerateToken("https://instance-a.example.com", "Instance A");

        Assert.NotEmpty(token);
        // Should be valid base64
        var bytes = Convert.FromBase64String(token);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateToken_ContainsUrlAndName()
    {
        var token = _service.GenerateToken("https://instance-a.example.com", "Instance A");

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var doc = JsonDocument.Parse(json);
        Assert.Equal("https://instance-a.example.com", doc.RootElement.GetProperty("url").GetString());
        Assert.Equal("Instance A", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void GenerateToken_ContainsSecretAndExpiry()
    {
        var token = _service.GenerateToken("https://example.com", "Test");

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("secret", out var secret));
        Assert.NotEmpty(secret.GetString()!);
        Assert.True(doc.RootElement.TryGetProperty("expiresAt", out var expires));
        var expiresAt = DateTime.Parse(expires.GetString()!);
        Assert.True(expiresAt > DateTime.UtcNow);
        Assert.True(expiresAt <= DateTime.UtcNow.AddMinutes(11)); // within 11 min
    }

    [Fact]
    public void DecodeToken_ValidToken_ReturnsPairingData()
    {
        var token = _service.GenerateToken("https://example.com", "Test Peer");

        var result = _service.DecodeToken(token);

        Assert.NotNull(result);
        Assert.Equal("https://example.com", result!.Url);
        Assert.Equal("Test Peer", result.Name);
        Assert.NotEmpty(result.Secret);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void DecodeToken_InvalidBase64_ReturnsNull()
    {
        var result = _service.DecodeToken("not-valid-base64!!!");

        Assert.Null(result);
    }

    [Fact]
    public void DecodeToken_InvalidJson_ReturnsNull()
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("not json"));

        var result = _service.DecodeToken(token);

        Assert.Null(result);
    }

    [Fact]
    public void ValidateSecret_MatchingSecret_ReturnsTrue()
    {
        var token = _service.GenerateToken("https://example.com", "Test");
        var data = _service.DecodeToken(token)!;

        var isValid = _service.ValidateSecret(data.Secret);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateSecret_UnknownSecret_ReturnsFalse()
    {
        var isValid = _service.ValidateSecret("unknown-secret");

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSecret_SameSecretTwice_ReturnsFalseSecondTime()
    {
        var token = _service.GenerateToken("https://example.com", "Test");
        var data = _service.DecodeToken(token)!;

        var first = _service.ValidateSecret(data.Secret);
        var second = _service.ValidateSecret(data.Secret);

        Assert.True(first);
        Assert.False(second); // one-time use
    }

    [Fact]
    public void DecodeToken_ExpiredToken_ReturnsDataWithPastExpiry()
    {
        // Create a manually expired token
        var payload = new
        {
            url = "https://example.com",
            name = "Expired",
            secret = "test-secret",
            expiresAt = DateTime.UtcNow.AddMinutes(-5).ToString("O")
        };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));

        var result = _service.DecodeToken(token);

        Assert.NotNull(result);
        Assert.True(result!.ExpiresAt < DateTime.UtcNow);
    }

    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var token = _service.GenerateToken("https://example.com", "Test");
        var data = _service.DecodeToken(token)!;

        Assert.False(data.ExpiresAt < DateTime.UtcNow);
    }
}
