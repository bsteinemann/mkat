using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Application.DTOs;
using Mkat.Application.Services;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class PeersControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _authClient;
    private readonly HttpClient _noAuthClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PeersControllerTests()
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

        _noAuthClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _noAuthClient.Dispose();
        _authClient.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    [Fact]
    public async Task Initiate_Authenticated_ReturnsToken()
    {
        var request = new PeerInitiateRequest { Name = "My Instance" };

        var response = await _authClient.PostAsJsonAsync("/api/v1/peers/pair/initiate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PeerInitiateResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Token);
    }

    [Fact]
    public async Task Initiate_Unauthenticated_Returns401()
    {
        var request = new PeerInitiateRequest { Name = "My Instance" };

        var response = await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/initiate", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accept_ValidSecret_ReturnsTokens()
    {
        // Generate a token first (this stores the secret in memory)
        var pairingService = _factory.Services.GetRequiredService<IPairingService>();
        var baseUrl = _authClient.BaseAddress!.ToString().TrimEnd('/');
        var token = pairingService.GenerateToken(baseUrl, "Instance A");
        var data = pairingService.DecodeToken(token)!;

        var acceptRequest = new PeerAcceptRequest
        {
            Secret = data.Secret,
            Url = "https://instance-b.example.com",
            Name = "Instance B"
        };

        // Accept endpoint does NOT require Basic Auth
        var response = await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", acceptRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PeerAcceptResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.HeartbeatToken);
        Assert.NotEmpty(body.WebhookToken);
        Assert.Equal(30, body.HeartbeatIntervalSeconds);
    }

    [Fact]
    public async Task Accept_InvalidSecret_Returns401()
    {
        var acceptRequest = new PeerAcceptRequest
        {
            Secret = "invalid-secret",
            Url = "https://instance-b.example.com",
            Name = "Instance B"
        };

        var response = await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", acceptRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accept_CreatesServiceAndMonitors()
    {
        var pairingService = _factory.Services.GetRequiredService<IPairingService>();
        var baseUrl = _authClient.BaseAddress!.ToString().TrimEnd('/');
        var token = pairingService.GenerateToken(baseUrl, "Instance A");
        var data = pairingService.DecodeToken(token)!;

        var acceptRequest = new PeerAcceptRequest
        {
            Secret = data.Secret,
            Url = "https://instance-b.example.com",
            Name = "Instance B"
        };

        await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", acceptRequest);

        // Get the peer list to find the service ID
        var peersResponse = await _authClient.GetAsync("/api/v1/peers");
        var peers = await peersResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var peer = peers.EnumerateArray().First();
        var serviceId = peer.GetProperty("serviceId").GetString();

        // Fetch the individual service (includes monitors)
        var serviceResponse = await _authClient.GetAsync($"/api/v1/services/{serviceId}");
        var service = await serviceResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var monitors = service.GetProperty("monitors");
        Assert.Equal(2, monitors.GetArrayLength()); // heartbeat + webhook
    }

    [Fact]
    public async Task Accept_CreatesPeerEntity()
    {
        var pairingService = _factory.Services.GetRequiredService<IPairingService>();
        var baseUrl = _authClient.BaseAddress!.ToString().TrimEnd('/');
        var token = pairingService.GenerateToken(baseUrl, "Instance A");
        var data = pairingService.DecodeToken(token)!;

        var acceptRequest = new PeerAcceptRequest
        {
            Secret = data.Secret,
            Url = "https://instance-b.example.com",
            Name = "Instance B"
        };

        await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", acceptRequest);

        // Verify peer is listed
        var peersResponse = await _authClient.GetAsync("/api/v1/peers");
        Assert.Equal(HttpStatusCode.OK, peersResponse.StatusCode);
        var peers = await peersResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(peers.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Initiate_EmptyName_ReturnsBadRequest()
    {
        var request = new PeerInitiateRequest { Name = "" };

        var response = await _authClient.PostAsJsonAsync("/api/v1/peers/pair/initiate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unpair_ExistingPeer_ReturnsNoContent()
    {
        // Create a peer via accept
        var pairingService = _factory.Services.GetRequiredService<IPairingService>();
        var baseUrl = _authClient.BaseAddress!.ToString().TrimEnd('/');
        var token = pairingService.GenerateToken(baseUrl, "Instance A");
        var data = pairingService.DecodeToken(token)!;

        await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", new PeerAcceptRequest
        {
            Secret = data.Secret,
            Url = "https://instance-b.example.com",
            Name = "Instance B"
        });

        // Get peer ID
        var peersResponse = await _authClient.GetAsync("/api/v1/peers");
        var peers = await peersResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var peerId = peers.EnumerateArray().First().GetProperty("id").GetString();

        // Unpair
        var response = await _authClient.DeleteAsync($"/api/v1/peers/{peerId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify peer is gone
        var afterResponse = await _authClient.GetAsync("/api/v1/peers");
        var afterPeers = await afterResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(0, afterPeers.GetArrayLength());
    }

    [Fact]
    public async Task Unpair_NonexistentPeer_Returns404()
    {
        var response = await _authClient.DeleteAsync($"/api/v1/peers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_EmptyUrl_ReturnsBadRequest()
    {
        var pairingService = _factory.Services.GetRequiredService<IPairingService>();
        var token = pairingService.GenerateToken("https://example.com", "A");
        var data = pairingService.DecodeToken(token)!;

        var acceptRequest = new PeerAcceptRequest
        {
            Secret = data.Secret,
            Url = "",
            Name = "B"
        };

        var response = await _noAuthClient.PostAsJsonAsync("/api/v1/peers/pair/accept", acceptRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
