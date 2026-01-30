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
public class ServiceUptimeControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;

    public ServiceUptimeControllerTests()
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

    [Fact]
    public async Task GetUptime_ReturnsUptimePercentage()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service { Id = Guid.NewGuid(), Name = "TestSvc" };
        var monitor = new Monitor { Id = Guid.NewGuid(), ServiceId = service.Id, Type = MonitorType.HealthCheck, Token = Guid.NewGuid().ToString() };

        context.Services.Add(service);
        context.Monitors.Add(monitor);

        context.MonitorEvents.AddRange(
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.HealthCheckPerformed, Success = true, CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.HealthCheckPerformed, Success = true, CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new MonitorEvent { Id = Guid.NewGuid(), MonitorId = monitor.Id, ServiceId = service.Id, EventType = EventType.HealthCheckPerformed, Success = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var response = await _authClient.GetAsync($"/api/v1/services/{service.Id}/uptime");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(service.Id.ToString(), root.GetProperty("serviceId").GetString());
        Assert.True(root.GetProperty("uptimePercent").GetDouble() > 0);
        Assert.Equal(3, root.GetProperty("totalEvents").GetInt32());
    }

    [Fact]
    public async Task GetUptime_InvalidService_ReturnsNotFound()
    {
        var response = await _authClient.GetAsync($"/api/v1/services/{Guid.NewGuid()}/uptime");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
