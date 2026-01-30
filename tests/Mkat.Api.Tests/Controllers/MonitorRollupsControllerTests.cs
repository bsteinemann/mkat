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
public class MonitorRollupsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MonitorRollupsControllerTests()
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
    public async Task GetMonitorRollups_ReturnsRollups()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MkatDbContext>();

        var service = new Service { Id = Guid.NewGuid(), Name = "TestSvc" };
        var monitor = new Monitor { Id = Guid.NewGuid(), ServiceId = service.Id, Type = MonitorType.HealthCheck, Token = Guid.NewGuid().ToString() };

        context.Services.Add(service);
        context.Monitors.Add(monitor);
        context.MonitorRollups.Add(new MonitorRollup
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            ServiceId = service.Id,
            Granularity = Granularity.Hourly,
            PeriodStart = DateTime.UtcNow.AddHours(-1),
            Count = 10,
            SuccessCount = 9,
            FailureCount = 1
        });
        await context.SaveChangesAsync();

        var response = await _authClient.GetAsync($"/api/v1/monitors/{monitor.Id}/rollups?granularity=Hourly");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetMonitorRollups_InvalidMonitor_ReturnsNotFound()
    {
        var response = await _authClient.GetAsync($"/api/v1/monitors/{Guid.NewGuid()}/rollups");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
