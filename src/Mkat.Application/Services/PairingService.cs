using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mkat.Application.Services;

public class PairingTokenData
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public interface IPairingService
{
    string GenerateToken(string instanceUrl, string instanceName);
    PairingTokenData? DecodeToken(string token);
    bool ValidateSecret(string secret);
}

public class PairingService : IPairingService
{
    private readonly ConcurrentDictionary<string, DateTime> _pendingSecrets = new();

    public string GenerateToken(string instanceUrl, string instanceName)
    {
        var secret = GenerateSecret();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        _pendingSecrets[secret] = expiresAt;

        var payload = new
        {
            url = instanceUrl,
            name = instanceName,
            secret,
            expiresAt = expiresAt.ToString("O")
        };

        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public PairingTokenData? DecodeToken(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new PairingTokenData
            {
                Url = root.GetProperty("url").GetString() ?? string.Empty,
                Name = root.GetProperty("name").GetString() ?? string.Empty,
                Secret = root.GetProperty("secret").GetString() ?? string.Empty,
                ExpiresAt = DateTime.Parse(root.GetProperty("expiresAt").GetString() ?? "")
            };
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateSecret(string secret)
    {
        if (!_pendingSecrets.TryRemove(secret, out var expiresAt))
            return false;

        return expiresAt > DateTime.UtcNow;
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
