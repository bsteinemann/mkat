using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Application.DTOs;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class AlertsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _unauthClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AlertsControllerTests()
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

        _unauthClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _unauthClient.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
    }

    private async Task<(Guid ServiceId, string Token)> CreateServiceWithAlertAsync()
    {
        var request = new CreateServiceRequest
        {
            Name = $"Alert Test {Guid.NewGuid():N}",
            Severity = Severity.High,
            Monitors = new List<CreateMonitorRequest>
            {
                new() { Type = MonitorType.Webhook, IntervalSeconds = 300 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/services", request);
        var service = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>(JsonOptions);
        var token = service!.Monitors[0].Token;

        // Trigger a failure to create an alert
        await _unauthClient.PostAsync($"/webhook/{token}/fail", null);

        return (service.Id, token);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyPage_WhenNoAlerts()
    {
        var response = await _client.GetAsync("/api/v1/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    public async Task GetAll_ReturnsAlerts()
    {
        await CreateServiceWithAlertAsync();

        var response = await _client.GetAsync("/api/v1/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal(AlertType.Failure, body.Items[0].Type);
    }

    [Fact]
    public async Task GetAll_SupportsPagination()
    {
        // Create multiple alerts
        await CreateServiceWithAlertAsync();
        await CreateServiceWithAlertAsync();
        await CreateServiceWithAlertAsync();

        var response = await _client.GetAsync("/api/v1/alerts?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(3, body.TotalCount);
        Assert.True(body.HasNextPage);
    }

    [Fact]
    public async Task GetById_ReturnsAlert_WhenExists()
    {
        await CreateServiceWithAlertAsync();

        var listResponse = await _client.GetAsync("/api/v1/alerts");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        var alertId = list!.Items[0].Id;

        var response = await _client.GetAsync($"/api/v1/alerts/{alertId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(alertId, body.Id);
        Assert.Equal(AlertType.Failure, body.Type);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/v1/alerts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Acknowledge_Returns200_AndSetsAcknowledgedAt()
    {
        await CreateServiceWithAlertAsync();

        var listResponse = await _client.GetAsync("/api/v1/alerts");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        var alertId = list!.Items[0].Id;

        var response = await _client.PostAsync($"/api/v1/alerts/{alertId}/ack", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("acknowledged").GetBoolean());

        // Verify it's acknowledged
        var getResponse = await _client.GetAsync($"/api/v1/alerts/{alertId}");
        var alert = await getResponse.Content.ReadFromJsonAsync<AlertResponse>(JsonOptions);
        Assert.NotNull(alert!.AcknowledgedAt);
    }

    [Fact]
    public async Task Acknowledge_Returns400_WhenAlreadyAcknowledged()
    {
        await CreateServiceWithAlertAsync();

        var listResponse = await _client.GetAsync("/api/v1/alerts");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>(JsonOptions);
        var alertId = list!.Items[0].Id;

        await _client.PostAsync($"/api/v1/alerts/{alertId}/ack", null);
        var response = await _client.PostAsync($"/api/v1/alerts/{alertId}/ack", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Acknowledge_Returns404_WhenNotExists()
    {
        var response = await _client.PostAsync($"/api/v1/alerts/{Guid.NewGuid()}/ack", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Alerts_RequireAuth()
    {
        var response = await _unauthClient.GetAsync("/api/v1/alerts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
