using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mkat.Api.Middleware;
using Xunit;

namespace Mkat.Api.Tests.Middleware;

[Collection("BasicAuth")]
public class BasicAuthMiddlewareTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public BasicAuthMiddlewareTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "secret123");

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<BasicAuthMiddleware>();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/api/v1/services", () => Results.Ok("authenticated"));
                            endpoints.MapGet("/health", () => Results.Ok("healthy"));
                            endpoints.MapGet("/health/ready", () => Results.Ok("ready"));
                            endpoints.MapPost("/webhook/token123/fail", () => Results.Ok("webhook"));
                            endpoints.MapPost("/heartbeat/token123", () => Results.Ok("heartbeat"));
                        });
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    [Fact]
    public async Task Request_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Basic", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Request_WithInvalidCredentials_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("wrong:wrong")));

        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidCredentials_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:secret123")));

        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithNonBasicScheme_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "sometoken");

        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutAuth_Returns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReadyEndpoint_WithoutAuth_Returns200()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WebhookEndpoint_WithoutAuth_Returns200()
    {
        var response = await _client.PostAsync("/webhook/token123/fail", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HeartbeatEndpoint_WithoutAuth_Returns200()
    {
        var response = await _client.PostAsync("/heartbeat/token123", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Collection("BasicAuth")]
public class BasicAuthMiddlewareNoPasswordTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public BasicAuthMiddlewareNoPasswordTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<BasicAuthMiddleware>();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/api/v1/services", () => Results.Ok("authenticated"));
                        });
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    [Fact]
    public async Task Request_WhenPasswordNotConfigured_Returns500()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:anything")));

        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
