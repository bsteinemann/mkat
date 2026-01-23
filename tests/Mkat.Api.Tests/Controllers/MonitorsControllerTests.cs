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
public class MonitorsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MonitorsControllerTests()
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
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123")));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<ServiceResponse> CreateTestServiceAsync()
    {
        var request = new CreateServiceRequest
        {
            Name = $"Test Service {Guid.NewGuid():N}",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task AddMonitor_ValidData_Returns201()
    {
        var service = await CreateTestServiceAsync();
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Webhook,
            IntervalSeconds = 600,
            GracePeriodSeconds = 120
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{service.Id}/monitors", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MonitorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(MonitorType.Webhook, body.Type);
        Assert.Equal(600, body.IntervalSeconds);
        Assert.Equal(120, body.GracePeriodSeconds);
        Assert.NotEmpty(body.Token);
    }

    [Fact]
    public async Task AddMonitor_InvalidInterval_Returns400()
    {
        var service = await CreateTestServiceAsync();
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Heartbeat,
            IntervalSeconds = 10 // below minimum of 30
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{service.Id}/monitors", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Code);
    }

    [Fact]
    public async Task AddMonitor_NonExistingService_Returns404()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.Webhook,
            IntervalSeconds = 300
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{Guid.NewGuid()}/monitors", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMonitor_ValidData_Returns200()
    {
        var service = await CreateTestServiceAsync();
        var monitorId = service.Monitors[0].Id;
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 600,
            GracePeriodSeconds = 120
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{service.Id}/monitors/{monitorId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MonitorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(600, body.IntervalSeconds);
        Assert.Equal(120, body.GracePeriodSeconds);
    }

    [Fact]
    public async Task UpdateMonitor_NonExistingMonitor_Returns404()
    {
        var service = await CreateTestServiceAsync();
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 600
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{service.Id}/monitors/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMonitor_MonitorNotOnService_Returns404()
    {
        var service1 = await CreateTestServiceAsync();
        var service2 = await CreateTestServiceAsync();
        var monitorFromService2 = service2.Monitors[0].Id;
        var request = new UpdateMonitorRequest
        {
            IntervalSeconds = 600
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{service1.Id}/monitors/{monitorFromService2}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMonitor_Returns204()
    {
        var service = await CreateTestServiceAsync();
        // Add a second monitor so we can delete one
        var addRequest = new AddMonitorRequest
        {
            Type = MonitorType.Webhook,
            IntervalSeconds = 300
        };
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/v1/services/{service.Id}/monitors", addRequest);
        addResponse.EnsureSuccessStatusCode();

        var monitorToDelete = service.Monitors[0].Id;

        var response = await _client.DeleteAsync(
            $"/api/v1/services/{service.Id}/monitors/{monitorToDelete}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMonitor_LastMonitor_Returns400()
    {
        var service = await CreateTestServiceAsync();
        var monitorId = service.Monitors[0].Id;

        var response = await _client.DeleteAsync(
            $"/api/v1/services/{service.Id}/monitors/{monitorId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("LAST_MONITOR", body.Code);
    }

    [Fact]
    public async Task DeleteMonitor_NonExisting_Returns404()
    {
        var service = await CreateTestServiceAsync();

        var response = await _client.DeleteAsync(
            $"/api/v1/services/{service.Id}/monitors/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
