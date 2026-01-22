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
public class WebhookControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WebhookControllerTests()
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

    private async Task<(Guid ServiceId, string Token)> CreateWebhookServiceAsync()
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

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return (body!.Id, body.Monitors[0].Token);
    }

    [Fact]
    public async Task Fail_WithValidToken_Returns200()
    {
        var (_, token) = await CreateWebhookServiceAsync();

        var response = await _client.PostAsync($"/webhook/{token}/fail", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("received").GetBoolean());
    }

    [Fact]
    public async Task Fail_WithUnknownToken_Returns404()
    {
        var response = await _client.PostAsync("/webhook/nonexistent-token/fail", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fail_TransitionsServiceToDown()
    {
        var (serviceId, token) = await CreateWebhookServiceAsync();

        await _client.PostAsync($"/webhook/{token}/fail", null);

        var getResponse = await _authClient.GetAsync($"/api/v1/services/{serviceId}");
        var service = await getResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.Equal(ServiceState.Down, service!.State);
    }

    [Fact]
    public async Task Fail_CreatesAlert()
    {
        var (_, token) = await CreateWebhookServiceAsync();

        var response = await _client.PostAsync($"/webhook/{token}/fail", null);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("alertCreated").GetBoolean());
    }

    [Fact]
    public async Task Fail_DuplicateTransition_NoNewAlert()
    {
        var (_, token) = await CreateWebhookServiceAsync();

        // First fail
        await _client.PostAsync($"/webhook/{token}/fail", null);
        // Second fail - should not create alert
        var response = await _client.PostAsync($"/webhook/{token}/fail", null);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("alertCreated").GetBoolean());
    }

    [Fact]
    public async Task Recover_WithValidToken_Returns200()
    {
        var (_, token) = await CreateWebhookServiceAsync();
        // First put it DOWN
        await _client.PostAsync($"/webhook/{token}/fail", null);

        var response = await _client.PostAsync($"/webhook/{token}/recover", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("received").GetBoolean());
    }

    [Fact]
    public async Task Recover_TransitionsServiceToUp()
    {
        var (serviceId, token) = await CreateWebhookServiceAsync();
        await _client.PostAsync($"/webhook/{token}/fail", null);

        await _client.PostAsync($"/webhook/{token}/recover", null);

        var getResponse = await _authClient.GetAsync($"/api/v1/services/{serviceId}");
        var service = await getResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.Equal(ServiceState.Up, service!.State);
    }

    [Fact]
    public async Task Recover_FromDown_CreatesRecoveryAlert()
    {
        var (_, token) = await CreateWebhookServiceAsync();
        await _client.PostAsync($"/webhook/{token}/fail", null);

        var response = await _client.PostAsync($"/webhook/{token}/recover", null);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("alertCreated").GetBoolean());
    }

    [Fact]
    public async Task Recover_WithUnknownToken_Returns404()
    {
        var response = await _client.PostAsync("/webhook/nonexistent-token/recover", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_DoesNotRequireAuth()
    {
        var (_, token) = await CreateWebhookServiceAsync();

        // Client without auth header
        var response = await _client.PostAsync($"/webhook/{token}/fail", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Fail_WithHeartbeatMonitorToken_Returns400()
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
        var createResponse = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        var body = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        var token = body!.Monitors[0].Token;

        var response = await _client.PostAsync($"/webhook/{token}/fail", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
