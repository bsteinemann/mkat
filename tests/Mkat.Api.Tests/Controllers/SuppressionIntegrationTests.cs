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
public class SuppressionIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _unauthClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SuppressionIntegrationTests()
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
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123")));

        _unauthClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _unauthClient.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<(Guid ServiceId, string Token)> CreateWebhookServiceAsync(string name)
    {
        var request = new CreateServiceRequest
        {
            Name = name,
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Webhook, IntervalSeconds = 300 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return (body!.Id, body.Monitors[0].Token);
    }

    private async Task AddDependencyAsync(Guid serviceId, Guid dependencyServiceId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceId}/dependencies",
            new AddDependencyRequest { DependencyServiceId = dependencyServiceId });
        response.EnsureSuccessStatusCode();
    }

    private async Task FailServiceAsync(string token)
    {
        var response = await _unauthClient.PostAsync($"/webhook/{token}/fail", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task RecoverServiceAsync(string token)
    {
        var response = await _unauthClient.PostAsync($"/webhook/{token}/recover", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<ServiceResponse> GetServiceAsync(Guid serviceId)
    {
        var response = await _client.GetAsync($"/api/v1/services/{serviceId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return body!;
    }

    [Fact]
    public async Task DownService_SuppressesTransitiveDependents()
    {
        // Arrange: A and B with webhook monitors, B depends on A
        var (serviceAId, tokenA) = await CreateWebhookServiceAsync("Service A");
        var (serviceBId, _) = await CreateWebhookServiceAsync("Service B");
        await AddDependencyAsync(serviceBId, serviceAId);

        // Act: Fail A
        await FailServiceAsync(tokenA);

        // Assert: B should be suppressed
        var serviceB = await GetServiceAsync(serviceBId);
        Assert.True(serviceB.IsSuppressed);
        Assert.NotNull(serviceB.SuppressionReason);
        Assert.Contains("Service A", serviceB.SuppressionReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuppressedService_DoesNotGenerateAlert()
    {
        // Arrange: A and B with webhook monitors, B depends on A
        var (serviceAId, tokenA) = await CreateWebhookServiceAsync("Service A");
        var (serviceBId, tokenB) = await CreateWebhookServiceAsync("Service B");
        await AddDependencyAsync(serviceBId, serviceAId);

        // Act: Fail A (suppresses B), then fail B
        await FailServiceAsync(tokenA);
        await FailServiceAsync(tokenB);

        // Assert: Only 1 alert exists (for A going DOWN), not for B
        var alertsResponse = await _client.GetAsync("/api/v1/alerts?page=1&pageSize=100");
        alertsResponse.EnsureSuccessStatusCode();
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        Assert.NotNull(alerts);
        Assert.Single(alerts.Items);
        Assert.Equal(AlertType.Failure, alerts.Items[0].Type);
    }

    [Fact]
    public async Task RecoveredDependency_ClearsSuppression()
    {
        // Arrange: A and B, B depends on A, A is down (B suppressed)
        var (serviceAId, tokenA) = await CreateWebhookServiceAsync("Service A");
        var (serviceBId, _) = await CreateWebhookServiceAsync("Service B");
        await AddDependencyAsync(serviceBId, serviceAId);
        await FailServiceAsync(tokenA);

        // Verify B is suppressed first
        var suppressedB = await GetServiceAsync(serviceBId);
        Assert.True(suppressedB.IsSuppressed);

        // Act: Recover A
        await RecoverServiceAsync(tokenA);

        // Assert: B should no longer be suppressed
        var serviceB = await GetServiceAsync(serviceBId);
        Assert.False(serviceB.IsSuppressed);
    }

    [Fact]
    public async Task TransitiveChain_SuppressesAllDependents()
    {
        // Arrange: A, B, C. B depends on A, C depends on B (chain: C → B → A)
        var (serviceAId, tokenA) = await CreateWebhookServiceAsync("Service A");
        var (serviceBId, _) = await CreateWebhookServiceAsync("Service B");
        var (serviceCId, _) = await CreateWebhookServiceAsync("Service C");
        await AddDependencyAsync(serviceBId, serviceAId);
        await AddDependencyAsync(serviceCId, serviceBId);

        // Act: Fail A
        await FailServiceAsync(tokenA);

        // Assert: Both B and C should be suppressed
        var serviceB = await GetServiceAsync(serviceBId);
        Assert.True(serviceB.IsSuppressed);

        var serviceC = await GetServiceAsync(serviceCId);
        Assert.True(serviceC.IsSuppressed);
    }
}
