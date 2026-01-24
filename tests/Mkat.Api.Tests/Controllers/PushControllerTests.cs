using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class PushControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PushControllerTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "test123");
        Environment.SetEnvironmentVariable("MKAT_VAPID_PUBLIC_KEY", "test-public-key");
        Environment.SetEnvironmentVariable("MKAT_VAPID_PRIVATE_KEY", "test-private-key");
        Environment.SetEnvironmentVariable("MKAT_VAPID_SUBJECT", "mailto:test@example.com");

        var dbName = $"TestDb_{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MkatDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<MkatDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    var hostedServices = services.Where(
                        d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                    foreach (var svc in hostedServices) services.Remove(svc);
                });
            });

        _client = _factory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    [Fact]
    public async Task Subscribe_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/sub1",
            keys = new { p256dh = "BNcRd...", auth = "tBHI..." }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Subscribe_PersistsSubscription()
    {
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/sub2",
            keys = new { p256dh = "key2", auth = "auth2" }
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == "https://push.example.com/sub2");
        Assert.NotNull(sub);
        Assert.Equal("key2", sub.P256dhKey);
    }

    [Fact]
    public async Task Subscribe_DuplicateEndpoint_ReturnsOk()
    {
        var body = new { endpoint = "https://push.example.com/dup", keys = new { p256dh = "k", auth = "a" } };
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", body);
        var response = await _client.PostAsJsonAsync("/api/v1/push/subscribe", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_RemovesSubscription()
    {
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/remove-me",
            keys = new { p256dh = "k", auth = "a" }
        });

        var response = await _client.PostAsJsonAsync("/api/v1/push/unsubscribe", new
        {
            endpoint = "https://push.example.com/remove-me"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == "https://push.example.com/remove-me");
        Assert.Null(sub);
    }

    [Fact]
    public async Task GetVapidPublicKey_ReturnsKey()
    {
        var response = await _client.GetAsync("/api/v1/push/vapid-public-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("publicKey"));
        Assert.Equal("test-public-key", body["publicKey"]);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
