using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class EventsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventsControllerTests()
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
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<MkatDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    var hostedServices = services.Where(
                        d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                    foreach (var svc in hostedServices) services.Remove(svc);
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    [Fact]
    public async Task Stream_ReturnsTextEventStream_ContentType()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/events/stream");

        var response = await _client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Stream_ReceivesBroadcastedEvent()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/events/stream");

        var response = await _client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cts.Token);

        // Broadcast an event
        var broadcaster = _factory.Services.GetRequiredService<IEventBroadcaster>();
        await Task.Delay(100);
        await broadcaster.BroadcastAsync(new ServerEvent
        {
            Type = "alert_created",
            Payload = "{\"id\":\"123\"}"
        });

        var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        var lines = new List<string>();
        while (!cts.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cts.Token);
            if (line == null) break;
            lines.Add(line);
            if (line.StartsWith("data:")) break;
        }

        Assert.Contains(lines, l => l.StartsWith("event: alert_created"));
        Assert.Contains(lines, l => l.Contains("\"id\":\"123\""));
    }

    [Fact]
    public async Task Stream_RequiresAuthentication()
    {
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/v1/events/stream");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
