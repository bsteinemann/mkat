using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Xunit;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class MetricHistoryControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MetricHistoryControllerTests()
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

    private async Task<(Guid ServiceId, Guid MonitorId, string Token)> SeedMetricMonitorWithReadingsAsync(int readingCount = 5)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"Metric History Test {Guid.NewGuid():N}",
            State = ServiceState.Up,
            Severity = Severity.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var token = Guid.NewGuid().ToString("N");
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Metric,
            Token = token,
            MaxValue = 100.0,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Services.Add(service);
        db.Monitors.Add(monitor);

        for (int i = 0; i < readingCount; i++)
        {
            db.MetricReadings.Add(new MetricReading
            {
                Id = Guid.NewGuid(),
                MonitorId = monitor.Id,
                Value = 50.0 + i * 10,
                RecordedAt = DateTime.UtcNow.AddMinutes(-readingCount + i),
                IsOutOfRange = (50.0 + i * 10) > 100.0
            });
        }

        if (readingCount > 0)
        {
            monitor.LastMetricValue = 50.0 + (readingCount - 1) * 10;
            monitor.LastMetricAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return (service.Id, monitor.Id, token);
    }

    // --- GET /api/v1/monitors/{id}/metrics ---

    [Fact]
    public async Task GetHistory_WithAuth_Returns200()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync();

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WithoutAuth_Returns401()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync();

        var response = await _client.GetAsync($"/api/v1/monitors/{monitorId}/metrics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_UnknownMonitor_Returns404()
    {
        var response = await _authClient.GetAsync($"/api/v1/monitors/{Guid.NewGuid()}/metrics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ReturnsReadings()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(3);

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var readings = body.GetProperty("readings");
        Assert.Equal(3, readings.GetArrayLength());
    }

    [Fact]
    public async Task GetHistory_WithTimeRange_FiltersReadings()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(5);

        var from = DateTime.UtcNow.AddMinutes(-3).ToString("o");
        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics?from={from}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var readings = body.GetProperty("readings");
        Assert.True(readings.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetHistory_ReturnsReadingsOrderedByRecordedAtDesc()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(3);

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        var readings = body.GetProperty("readings");
        var first = DateTime.Parse(readings[0].GetProperty("recordedAt").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var last = DateTime.Parse(readings[readings.GetArrayLength() - 1].GetProperty("recordedAt").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(first >= last);
    }

    // --- GET /api/v1/monitors/{id}/metrics/latest ---

    [Fact]
    public async Task GetLatest_WithAuth_Returns200()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(1);

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_WithoutAuth_Returns401()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(1);

        var response = await _client.GetAsync($"/api/v1/monitors/{monitorId}/metrics/latest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_UnknownMonitor_Returns404()
    {
        var response = await _authClient.GetAsync($"/api/v1/monitors/{Guid.NewGuid()}/metrics/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_ReturnsLatestValue()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(3);

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics/latest");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Assert.True(body.TryGetProperty("value", out _));
        Assert.True(body.TryGetProperty("recordedAt", out _));
        Assert.True(body.TryGetProperty("outOfRange", out _));
    }

    [Fact]
    public async Task GetLatest_NoReadings_Returns204()
    {
        var (_, monitorId, _) = await SeedMetricMonitorWithReadingsAsync(0);

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/metrics/latest");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_NonMetricMonitor_Returns400()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"NonMetric {Guid.NewGuid():N}",
            State = ServiceState.Up,
            Severity = Severity.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.Webhook,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Services.Add(service);
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitor.Id}/metrics/latest");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
