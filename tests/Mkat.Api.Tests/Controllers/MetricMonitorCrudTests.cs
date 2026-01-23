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
public class MetricMonitorCrudTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MetricMonitorCrudTests()
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
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<Guid> CreateServiceAsync()
    {
        var request = new CreateServiceRequest
        {
            Name = $"MetricCrud Test {Guid.NewGuid():N}",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 60 }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return body!.Id;
    }

    [Fact]
    public async Task AddMonitor_MetricType_ReturnsMetricFields()
    {
        var serviceId = await CreateServiceAsync();

        var request = new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            MaxValue = 100.0,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7
        };

        var response = await _authClient.PostAsJsonAsync($"/api/v1/services/{serviceId}/monitors", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal((int)MonitorType.Metric, body.GetProperty("type").GetInt32());
        Assert.Equal(100.0, body.GetProperty("maxValue").GetDouble());
        Assert.Equal((int)ThresholdStrategy.Immediate, body.GetProperty("thresholdStrategy").GetInt32());
        Assert.Equal(7, body.GetProperty("retentionDays").GetInt32());
        Assert.True(body.TryGetProperty("metricUrl", out var metricUrl));
        Assert.Contains("/metric/", metricUrl.GetString());
    }

    [Fact]
    public async Task AddMonitor_MetricType_WithAllFields()
    {
        var serviceId = await CreateServiceAsync();

        var request = new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            MinValue = 10.0,
            MaxValue = 90.0,
            ThresholdStrategy = ThresholdStrategy.ConsecutiveCount,
            ThresholdCount = 3,
            RetentionDays = 30
        };

        var response = await _authClient.PostAsJsonAsync($"/api/v1/services/{serviceId}/monitors", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(10.0, body.GetProperty("minValue").GetDouble());
        Assert.Equal(90.0, body.GetProperty("maxValue").GetDouble());
        Assert.Equal((int)ThresholdStrategy.ConsecutiveCount, body.GetProperty("thresholdStrategy").GetInt32());
        Assert.Equal(3, body.GetProperty("thresholdCount").GetInt32());
        Assert.Equal(30, body.GetProperty("retentionDays").GetInt32());
    }

    [Fact]
    public async Task AddMonitor_MetricType_ValidationFails_NoBounds()
    {
        var serviceId = await CreateServiceAsync();

        var request = new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7
        };

        var response = await _authClient.PostAsJsonAsync($"/api/v1/services/{serviceId}/monitors", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMonitor_MetricType_UpdatesFields()
    {
        var serviceId = await CreateServiceAsync();

        // First add a metric monitor
        var addRequest = new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            MaxValue = 100.0,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7
        };
        var addResponse = await _authClient.PostAsJsonAsync($"/api/v1/services/{serviceId}/monitors", addRequest);
        var addBody = await addResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var monitorId = addBody.GetProperty("id").GetString();

        // Now update with new metric config
        var updateRequest = new UpdateMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 120,
            MinValue = 20.0,
            MaxValue = 80.0,
            ThresholdStrategy = ThresholdStrategy.ConsecutiveCount,
            ThresholdCount = 5,
            RetentionDays = 14
        };

        var response = await _authClient.PutAsJsonAsync($"/api/v1/services/{serviceId}/monitors/{monitorId}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(20.0, body.GetProperty("minValue").GetDouble());
        Assert.Equal(80.0, body.GetProperty("maxValue").GetDouble());
        Assert.Equal((int)ThresholdStrategy.ConsecutiveCount, body.GetProperty("thresholdStrategy").GetInt32());
        Assert.Equal(5, body.GetProperty("thresholdCount").GetInt32());
        Assert.Equal(14, body.GetProperty("retentionDays").GetInt32());
    }

    [Fact]
    public async Task CreateService_WithMetricMonitor_ReturnsMetricFields()
    {
        var request = new CreateServiceRequest
        {
            Name = $"MetricService {Guid.NewGuid():N}",
            Severity = Severity.High,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.Metric,
                    IntervalSeconds = 60,
                    MaxValue = 95.0,
                    ThresholdStrategy = ThresholdStrategy.Immediate,
                    RetentionDays = 7
                }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var monitors = body.GetProperty("monitors");
        Assert.Equal(1, monitors.GetArrayLength());
        var monitor = monitors[0];
        Assert.Equal((int)MonitorType.Metric, monitor.GetProperty("type").GetInt32());
        Assert.Equal(95.0, monitor.GetProperty("maxValue").GetDouble());
        Assert.True(monitor.TryGetProperty("metricUrl", out _));
    }

    [Fact]
    public async Task GetService_WithMetricMonitor_IncludesMetricFields()
    {
        var serviceId = await CreateServiceAsync();

        var addRequest = new AddMonitorRequest
        {
            Type = MonitorType.Metric,
            IntervalSeconds = 60,
            MaxValue = 100.0,
            ThresholdStrategy = ThresholdStrategy.Immediate,
            RetentionDays = 7
        };
        await _authClient.PostAsJsonAsync($"/api/v1/services/{serviceId}/monitors", addRequest);

        var response = await _authClient.GetAsync($"/api/v1/services/{serviceId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var monitors = body.GetProperty("monitors");

        // Find the metric monitor
        var metricMonitor = monitors.EnumerateArray().First(m => m.GetProperty("type").GetInt32() == (int)MonitorType.Metric);
        Assert.Equal(100.0, metricMonitor.GetProperty("maxValue").GetDouble());
        Assert.True(metricMonitor.TryGetProperty("metricUrl", out _));
    }
}
