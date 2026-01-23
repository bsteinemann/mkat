using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests;

[Collection("BasicAuth")]
public class BasePathTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BasePathTests()
    {
        Environment.SetEnvironmentVariable("MKAT_BASE_PATH", "/mkat");
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "testpass");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MkatDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<MkatDbContext>(options =>
                        options.UseInMemoryDatabase($"BasePathTests_{Guid.NewGuid()}"));

                    // Remove background workers to prevent resource contention in tests
                    var hostedServices = services.Where(
                        d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                    foreach (var svc in hostedServices)
                        services.Remove(svc);
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HealthEndpoint_WithBasePath_ReturnsOk()
    {
        var response = await _client.GetAsync("/mkat/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutBasePath_StillWorks()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithBasePath_RequiresAuth()
    {
        var response = await _client.GetAsync("/mkat/api/v1/services");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithBasePath_ReturnsData()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String("admin:testpass"u8.ToArray()));

        var response = await _client.GetAsync("/mkat/api/v1/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SpaFallback_WithBasePath_ReturnsHtmlWithConfig()
    {
        var response = await _client.GetAsync("/mkat/dashboard");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("__MKAT_BASE_PATH__", content);
        Assert.Contains("/mkat", content);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("MKAT_BASE_PATH", null);
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
        _client.Dispose();
        _factory.Dispose();
    }
}
