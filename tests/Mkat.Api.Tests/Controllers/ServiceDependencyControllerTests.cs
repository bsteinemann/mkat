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
public class ServiceDependencyControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ServiceDependencyControllerTests()
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

    private async Task<ServiceResponse> CreateServiceAsync(string name = "Test Service")
    {
        var request = new CreateServiceRequest
        {
            Name = name,
            Description = $"Description for {name}",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        return body!;
    }

    [Fact]
    public async Task AddDependency_ReturnsCreated()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DependencyResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(serviceB.Id, body.Id);
        Assert.Equal("Service B", body.Name);
    }

    [Fact]
    public async Task AddDependency_ReturnsBadRequest_OnCycle()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        // A depends on B
        var first = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // B depends on A — should fail (cycle)
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceB.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceA.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("DEPENDENCY_CYCLE", body.Code);
    }

    [Fact]
    public async Task AddDependency_ReturnsBadRequest_OnSelfReference()
    {
        var serviceA = await CreateServiceAsync("Service A");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceA.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("SELF_DEPENDENCY", body.Code);
    }

    [Fact]
    public async Task AddDependency_ReturnsConflict_OnDuplicate()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("DEPENDENCY_EXISTS", body.Code);
    }

    [Fact]
    public async Task GetDependencies_ReturnsAddedDependencies()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");
        var serviceC = await CreateServiceAsync("Service C");

        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceC.Id });

        var response = await _client.GetAsync($"/api/v1/services/{serviceA.Id}/dependencies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DependencyResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, d => d.Id == serviceB.Id);
        Assert.Contains(body, d => d.Id == serviceC.Id);
    }

    [Fact]
    public async Task GetDependents_ReturnsCorrectServices()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        // A depends on B, so B's dependents should include A
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        var response = await _client.GetAsync($"/api/v1/services/{serviceB.Id}/dependents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<DependencyResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal(serviceA.Id, body[0].Id);
        Assert.Equal("Service A", body[0].Name);
    }

    [Fact]
    public async Task DeleteDependency_RemovesEdge()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies/{serviceB.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/v1/services/{serviceA.Id}/dependencies");
        var deps = await getResponse.Content.ReadFromJsonAsync<List<DependencyResponse>>(JsonOptions);
        Assert.NotNull(deps);
        Assert.Empty(deps);
    }

    [Fact]
    public async Task GetGraph_ReturnsAllNodesAndEdges()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");
        var serviceC = await CreateServiceAsync("Service C");

        // A depends on B, B depends on C
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceB.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceC.Id });

        var response = await _client.GetAsync("/api/v1/services/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var graph = await response.Content.ReadFromJsonAsync<DependencyGraphResponse>(JsonOptions);
        Assert.NotNull(graph);
        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);

        // Verify nodes
        Assert.Contains(graph.Nodes, n => n.Id == serviceA.Id && n.Name == "Service A");
        Assert.Contains(graph.Nodes, n => n.Id == serviceB.Id && n.Name == "Service B");
        Assert.Contains(graph.Nodes, n => n.Id == serviceC.Id && n.Name == "Service C");

        // Verify edges
        Assert.Contains(graph.Edges, e => e.DependentId == serviceA.Id && e.DependencyId == serviceB.Id);
        Assert.Contains(graph.Edges, e => e.DependentId == serviceB.Id && e.DependencyId == serviceC.Id);
    }

    [Fact]
    public async Task ServiceResponse_IncludesDependencyInfo()
    {
        var serviceA = await CreateServiceAsync("Service A");
        var serviceB = await CreateServiceAsync("Service B");

        // A depends on B
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{serviceA.Id}/dependencies",
            new AddDependencyRequest { DependencyServiceId = serviceB.Id });

        // Get service A — should include B in DependsOn
        var responseA = await _client.GetAsync($"/api/v1/services/{serviceA.Id}");
        var bodyA = await responseA.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.NotNull(bodyA);
        Assert.Single(bodyA.DependsOn);
        Assert.Equal(serviceB.Id, bodyA.DependsOn[0].Id);
        Assert.Empty(bodyA.DependedOnBy);

        // Get service B — should include A in DependedOnBy
        var responseB = await _client.GetAsync($"/api/v1/services/{serviceB.Id}");
        var bodyB = await responseB.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.NotNull(bodyB);
        Assert.Empty(bodyB.DependsOn);
        Assert.Single(bodyB.DependedOnBy);
        Assert.Equal(serviceA.Id, bodyB.DependedOnBy[0].Id);
    }
}
