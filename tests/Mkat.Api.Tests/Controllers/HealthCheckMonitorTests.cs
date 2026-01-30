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
public class HealthCheckMonitorTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HealthCheckMonitorTests()
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

    [Fact]
    public async Task CreateService_WithHealthCheckMonitor_Succeeds()
    {
        var request = new CreateServiceRequest
        {
            Name = "HealthCheck Test Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60,
                    HealthCheckUrl = "https://example.com/health",
                    HttpMethod = "GET",
                    ExpectedStatusCodes = "200,201",
                    TimeoutSeconds = 15,
                    BodyMatchRegex = "ok|healthy"
                }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var service = JsonSerializer.Deserialize<ServiceResponse>(json, JsonOptions);
        Assert.NotNull(service);
        Assert.Single(service!.Monitors);
        var monitor = service.Monitors[0];
        Assert.Equal(MonitorType.HealthCheck, monitor.Type);
        Assert.Equal("https://example.com/health", monitor.HealthCheckUrl);
        Assert.Equal("GET", monitor.HttpMethod);
        Assert.Equal("200,201", monitor.ExpectedStatusCodes);
        Assert.Equal(15, monitor.TimeoutSeconds);
        Assert.Equal("ok|healthy", monitor.BodyMatchRegex);
    }

    [Fact]
    public async Task CreateService_HealthCheckWithoutUrl_Returns400()
    {
        var request = new CreateServiceRequest
        {
            Name = "Bad HealthCheck Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60
                }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateService_HealthCheckWithDefaults_UsesDefaults()
    {
        var request = new CreateServiceRequest
        {
            Name = "Default HealthCheck Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60,
                    HealthCheckUrl = "https://example.com/health"
                }
            }
        };

        var response = await _authClient.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var service = JsonSerializer.Deserialize<ServiceResponse>(json, JsonOptions);
        var monitor = service!.Monitors[0];
        Assert.Equal("GET", monitor.HttpMethod);
        Assert.Equal("200", monitor.ExpectedStatusCodes);
        Assert.Equal(10, monitor.TimeoutSeconds);
        Assert.Null(monitor.BodyMatchRegex);
    }
}
