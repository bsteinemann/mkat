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
public class ServicesControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ServicesControllerTests()
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

    private CreateServiceRequest ValidCreateRequest() => new()
    {
        Name = "Test Service",
        Description = "A test service",
        Severity = Severity.Medium,
        Monitors = new List<CreateMonitorRequest>
        {
            new() { Type = MonitorType.Heartbeat, IntervalSeconds = 300 }
        }
    };

    [Fact]
    public async Task CreateService_WithValidData_Returns201()
    {
        var request = ValidCreateRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Test Service", body.Name);
        Assert.Equal(ServiceState.Unknown, body.State);
        Assert.Single(body.Monitors);
        Assert.NotEmpty(body.Monitors[0].Token);
    }

    [Fact]
    public async Task CreateService_WithInvalidData_Returns400()
    {
        var request = new CreateServiceRequest
        {
            Name = "",
            Monitors = new List<CreateMonitorRequest>()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.NotNull(body.Details);
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyPage()
    {
        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ServiceResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithServices_ReturnsPaginatedList()
    {
        await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest() with { Name = "Second" });

        var response = await _client.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ServiceResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
    }

    [Fact]
    public async Task GetAll_WithPagination_RespectsPageSize()
    {
        await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest() with { Name = "A" });
        await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest() with { Name = "B" });
        await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest() with { Name = "C" });

        var response = await _client.GetAsync("/api/v1/services?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ServiceResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(3, body.TotalCount);
        Assert.True(body.HasNextPage);
    }

    [Fact]
    public async Task GetById_ExistingService_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var response = await _client.GetAsync($"/api/v1/services/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal("Test Service", body.Name);
    }

    [Fact]
    public async Task GetById_NonExistingService_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/services/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("SERVICE_NOT_FOUND", body.Code);
    }

    [Fact]
    public async Task Update_ExistingService_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var updateRequest = new UpdateServiceRequest
        {
            Name = "Updated Name",
            Description = "Updated desc",
            Severity = Severity.High
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/services/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Updated Name", body.Name);
        Assert.Equal(Severity.High, body.Severity);
    }

    [Fact]
    public async Task Update_NonExistingService_Returns404()
    {
        var updateRequest = new UpdateServiceRequest
        {
            Name = "Updated Name",
            Severity = Severity.Medium
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/services/{Guid.NewGuid()}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithInvalidData_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var updateRequest = new UpdateServiceRequest { Name = "", Severity = Severity.Medium };

        var response = await _client.PutAsJsonAsync($"/api/v1/services/{created!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingService_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var response = await _client.DeleteAsync($"/api/v1/services/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/services/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingService_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/services/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateService_MonitorUrls_AreGenerated()
    {
        var request = ValidCreateRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        var body = await response.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        Assert.NotNull(body);
        var monitor = body.Monitors[0];
        Assert.Contains("/webhook/", monitor.WebhookFailUrl);
        Assert.Contains("/fail", monitor.WebhookFailUrl);
        Assert.Contains("/webhook/", monitor.WebhookRecoverUrl);
        Assert.Contains("/recover", monitor.WebhookRecoverUrl);
        Assert.Contains("/heartbeat/", monitor.HeartbeatUrl);
    }

    [Fact]
    public async Task Request_WithoutAuth_Returns401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/services");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        unauthClient.Dispose();
    }

    // --- Pause/Resume ---

    [Fact]
    public async Task Pause_ExistingService_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{created!.Id}/pause",
            new { until = DateTime.UtcNow.AddHours(1), autoResume = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("paused").GetBoolean());
    }

    [Fact]
    public async Task Pause_SetsServiceStateToPaused()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        await _client.PostAsJsonAsync(
            $"/api/v1/services/{created!.Id}/pause",
            new { autoResume = false });

        var getResponse = await _client.GetAsync($"/api/v1/services/{created.Id}");
        var service = await getResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.Equal(ServiceState.Paused, service!.State);
    }

    [Fact]
    public async Task Pause_NonExistingService_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/services/{Guid.NewGuid()}/pause",
            new { autoResume = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Resume_PausedService_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{created!.Id}/pause",
            new { autoResume = false });

        var response = await _client.PostAsync($"/api/v1/services/{created.Id}/resume", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("resumed").GetBoolean());
    }

    [Fact]
    public async Task Resume_SetsServiceStateToUnknown()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        await _client.PostAsJsonAsync(
            $"/api/v1/services/{created!.Id}/pause",
            new { autoResume = false });

        await _client.PostAsync($"/api/v1/services/{created.Id}/resume", null);

        var getResponse = await _client.GetAsync($"/api/v1/services/{created.Id}");
        var service = await getResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        Assert.Equal(ServiceState.Unknown, service!.State);
    }

    [Fact]
    public async Task Resume_NonPausedService_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", ValidCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);

        var response = await _client.PostAsync($"/api/v1/services/{created!.Id}/resume", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resume_NonExistingService_Returns404()
    {
        var response = await _client.PostAsync($"/api/v1/services/{Guid.NewGuid()}/resume", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
