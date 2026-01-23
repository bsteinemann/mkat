using System.Net;
using System.Net.Http.Json;
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
public class MetricControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _dbName;

    public MetricControllerTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "test123");

        _dbName = $"TestDb_{Guid.NewGuid()}";
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
                        options.UseInMemoryDatabase(_dbName));

                    var hostedServices = services.Where(
                        d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                    foreach (var svc in hostedServices)
                        services.Remove(svc);
                });
            });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<(Guid ServiceId, Guid MonitorId, string Token)> SeedMetricMonitorAsync(
        double? minValue = null,
        double? maxValue = 100.0,
        ThresholdStrategy strategy = ThresholdStrategy.Immediate,
        int? thresholdCount = null,
        int? windowSeconds = null,
        int? windowSampleCount = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"Metric Test {Guid.NewGuid():N}",
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
            MinValue = minValue,
            MaxValue = maxValue,
            ThresholdStrategy = strategy,
            ThresholdCount = thresholdCount,
            WindowSeconds = windowSeconds,
            WindowSampleCount = windowSampleCount,
            RetentionDays = 7,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Services.Add(service);
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        return (service.Id, monitor.Id, token);
    }

    [Fact]
    public async Task Post_WithValidToken_Returns200()
    {
        var (_, _, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        var response = await _client.PostAsJsonAsync($"/metric/{token}", new { value = 50.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithQueryParam_Returns200()
    {
        var (_, _, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        var response = await _client.PostAsync($"/metric/{token}?value=50.0", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownToken_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/metric/nonexistent-token", new { value = 50.0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithNonMetricToken_Returns400()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = $"NonMetric Test {Guid.NewGuid():N}",
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
            Type = MonitorType.Webhook,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Services.Add(service);
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync($"/metric/{token}", new { value = 50.0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_NoValueProvided_Returns400()
    {
        var (_, _, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        var response = await _client.PostAsJsonAsync($"/metric/{token}", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_StoresReading()
    {
        var (_, monitorId, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 42.5 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var reading = await db.MetricReadings.FirstOrDefaultAsync(r => r.MonitorId == monitorId);
        Assert.NotNull(reading);
        Assert.Equal(42.5, reading.Value);
        Assert.False(reading.IsOutOfRange);
    }

    [Fact]
    public async Task Post_OutOfRange_MarksReadingAsOutOfRange()
    {
        var (_, monitorId, token) = await SeedMetricMonitorAsync(maxValue: 90.0);

        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 95.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var reading = await db.MetricReadings.FirstOrDefaultAsync(r => r.MonitorId == monitorId);
        Assert.NotNull(reading);
        Assert.True(reading.IsOutOfRange);
    }

    [Fact]
    public async Task Post_UpdatesLastMetricValueAndAt()
    {
        var (_, monitorId, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 55.5 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var monitor = await db.Monitors.FindAsync(monitorId);
        Assert.Equal(55.5, monitor!.LastMetricValue);
        Assert.NotNull(monitor.LastMetricAt);
    }

    [Fact]
    public async Task Post_Immediate_OutOfRange_TransitionsToDown()
    {
        var (serviceId, _, token) = await SeedMetricMonitorAsync(maxValue: 90.0);

        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 95.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        Assert.Equal(ServiceState.Down, service!.State);
    }

    [Fact]
    public async Task Post_Immediate_InRange_ServiceStaysUp()
    {
        var (serviceId, _, token) = await SeedMetricMonitorAsync(maxValue: 90.0);

        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 80.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        Assert.Equal(ServiceState.Up, service!.State);
    }

    [Fact]
    public async Task Post_Recovery_TransitionsBackToUp()
    {
        var (serviceId, _, token) = await SeedMetricMonitorAsync(maxValue: 90.0);

        // First, push out-of-range to go DOWN
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 95.0 });

        // Then push in-range to recover
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 80.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        Assert.Equal(ServiceState.Up, service!.State);
    }

    [Fact]
    public async Task Post_ConsecutiveCount_NotEnough_StaysUp()
    {
        var (serviceId, _, token) = await SeedMetricMonitorAsync(
            maxValue: 90.0,
            strategy: ThresholdStrategy.ConsecutiveCount,
            thresholdCount: 3);

        // Only 1 out-of-range reading
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 95.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        Assert.Equal(ServiceState.Up, service!.State);
    }

    [Fact]
    public async Task Post_ConsecutiveCount_EnoughConsecutive_GoesDown()
    {
        var (serviceId, _, token) = await SeedMetricMonitorAsync(
            maxValue: 90.0,
            strategy: ThresholdStrategy.ConsecutiveCount,
            thresholdCount: 3);

        // 3 consecutive out-of-range readings
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 95.0 });
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 92.0 });
        await _client.PostAsJsonAsync($"/metric/{token}", new { value = 91.0 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        Assert.Equal(ServiceState.Down, service!.State);
    }

    [Fact]
    public async Task Post_ReturnsMetricStatus()
    {
        var (_, _, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        var response = await _client.PostAsJsonAsync($"/metric/{token}", new { value = 50.0 });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("received").GetBoolean());
        Assert.Equal(50.0, body.GetProperty("value").GetDouble());
        Assert.False(body.GetProperty("outOfRange").GetBoolean());
    }

    [Fact]
    public async Task Post_DoesNotRequireAuth()
    {
        var (_, _, token) = await SeedMetricMonitorAsync(maxValue: 100.0);

        // Client has no auth header
        var response = await _client.PostAsJsonAsync($"/metric/{token}", new { value = 50.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
