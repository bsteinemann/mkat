using System.Net;
using System.Net.Http.Headers;
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
public class MonitorEventsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;
    private readonly string _dbName;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MonitorEventsControllerTests()
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

        _authClient = _factory.CreateClient();
        _authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123")));
    }

    public void Dispose()
    {
        _authClient.Dispose();
        _factory.Dispose();
    }

    private async Task<(Guid serviceId, Guid monitorId)> SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service { Id = Guid.NewGuid(), Name = "TestSvc" };
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            Type = MonitorType.HealthCheck,
            Token = Guid.NewGuid().ToString()
        };

        context.Services.Add(service);
        context.Monitors.Add(monitor);

        var events = new[]
        {
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.HealthCheckPerformed, Success = true, Value = 100, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.HealthCheckPerformed, Success = false, Value = 500, CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.StateChanged, Success = false, CreatedAt = DateTime.UtcNow }
        };

        context.MonitorEvents.AddRange(events);
        await context.SaveChangesAsync();

        return (service.Id, monitor.Id);
    }

    [Fact]
    public async Task GetMonitorEvents_ReturnsEvents()
    {
        var (_, monitorId) = await SeedTestData();

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() >= 3);
    }

    [Fact]
    public async Task GetMonitorEvents_FiltersByEventType()
    {
        var (_, monitorId) = await SeedTestData();

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitorId}/events?eventType=StateChanged");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetMonitorEvents_InvalidMonitor_ReturnsNotFound()
    {
        var response = await _authClient.GetAsync($"/api/v1/monitors/{Guid.NewGuid()}/events");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetServiceEvents_ReturnsEvents()
    {
        var (serviceId, _) = await SeedTestData();

        var response = await _authClient.GetAsync($"/api/v1/services/{serviceId}/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() >= 3);
    }
}
