using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Application.DTOs;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class HeartbeatControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HeartbeatControllerTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "test123");

        var dbName = $"TestDb_{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MkatDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<MkatDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    // Remove background workers to prevent resource contention in tests
                    var hostedServices = services.Where(
                        d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                    foreach (var svc in hostedServices)
                        services.Remove(svc);
                });
            });

        _client = _factory.CreateClient();
        _authClient = _factory.CreateClient();
        _authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123")));
    }

    public void Dispose()
    {
        _client.Dispose();
        _authClient.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<(Guid ServiceId, string Token)> CreateHeartbeatServiceAsync()
    {
        var request = new CreateServiceRequest
        {
            Name = $"Heartbeat Test {Guid.NewGuid():N}",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 60 }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return (body!.Id, body.Monitors[0].Token);
    }

    [Fact]
    public async Task Heartbeat_WithValidToken_Returns200()
    {
        var (_, token) = await CreateHeartbeatServiceAsync();

        var response = await _client.PostAsync($"/heartbeat/{token}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("received").GetBoolean());
    }

    [Fact]
    public async Task Heartbeat_WithUnknownToken_Returns404()
    {
        var response = await _client.PostAsync("/heartbeat/nonexistent-token", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_TransitionsServiceToUp()
    {
        var (serviceId, token) = await CreateHeartbeatServiceAsync();

        await _client.PostAsync($"/heartbeat/{token}", null);

        var getResponse = await _authClient.GetAsync($"/api/v1/services/{serviceId}");
        var service = await getResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.Equal(ServiceState.Up, service!.State);
    }

    [Fact]
    public async Task Heartbeat_AfterDown_CreatesRecoveryAlert()
    {
        var (serviceId, token) = await CreateHeartbeatServiceAsync();

        // First transition to DOWN via a webhook-style state change
        // We'll use a second webhook monitor for this, or manipulate state directly
        // Simplest: create a webhook monitor too
        // Actually, let's just use the state service indirectly:
        // Send heartbeat (UP), then we need to get it DOWN first.
        // Let's use the DB directly through the factory
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
            var service = await db.Services.FindAsync(serviceId);
            service!.State = ServiceState.Down;
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/heartbeat/{token}", null);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("alertCreated").GetBoolean());
    }

    [Fact]
    public async Task Heartbeat_ReturnsNextExpectedTime()
    {
        var (_, token) = await CreateHeartbeatServiceAsync();

        var response = await _client.PostAsync($"/heartbeat/{token}", null);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("nextExpectedBefore", out var next));
        var nextTime = next.GetDateTime();
        Assert.True(nextTime > DateTime.UtcNow);
    }

    [Fact]
    public async Task Heartbeat_DoesNotRequireAuth()
    {
        var (_, token) = await CreateHeartbeatServiceAsync();

        var response = await _client.PostAsync($"/heartbeat/{token}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_WithWebhookMonitorToken_Returns400()
    {
        var request = new CreateServiceRequest
        {
            Name = $"Webhook Test {Guid.NewGuid():N}",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Webhook, IntervalSeconds = 300 }
            }
        };
        var createResponse = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        var body = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        var token = body!.Monitors[0].Token;

        var response = await _client.PostAsync($"/heartbeat/{token}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
