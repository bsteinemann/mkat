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
public class ServiceContactsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ServiceContactsTests()
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
    }

    private async Task<Guid> CreateService(string name = "Test Service")
    {
        var request = new
        {
            Name = name,
            Severity = Severity.Medium,
            Monitors = new[] { new { Type = MonitorType.Heartbeat, IntervalSeconds = 60 } }
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/services", request);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return Guid.Parse(doc.RootElement.GetProperty("id").GetString()!);
    }

    private async Task<Guid> CreateContact(string name = "Test Contact")
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = name });
        var contact = await resp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        return contact!.Id;
    }

    [Fact]
    public async Task SetServiceContacts_ReturnsOk()
    {
        var serviceId = await CreateService();
        var contactId = await CreateContact("Alpha");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{serviceId}/contacts",
            new { ContactIds = new[] { contactId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetServiceContacts_EmptyList_ReturnsBadRequest()
    {
        var serviceId = await CreateService();

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{serviceId}/contacts",
            new { ContactIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetServiceContacts_ReturnsAssignedContacts()
    {
        var serviceId = await CreateService();
        var contactId1 = await CreateContact("First");
        var contactId2 = await CreateContact("Second");

        await _client.PutAsJsonAsync(
            $"/api/v1/services/{serviceId}/contacts",
            new { ContactIds = new[] { contactId1, contactId2 } });

        var response = await _client.GetAsync($"/api/v1/services/{serviceId}/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contacts = await response.Content.ReadFromJsonAsync<List<ContactResponse>>(JsonOptions);
        Assert.NotNull(contacts);
        Assert.Equal(2, contacts.Count);
    }

    [Fact]
    public async Task GetServiceContacts_NoAssignment_ReturnsEmpty()
    {
        var serviceId = await CreateService();

        var response = await _client.GetAsync($"/api/v1/services/{serviceId}/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contacts = await response.Content.ReadFromJsonAsync<List<ContactResponse>>(JsonOptions);
        Assert.NotNull(contacts);
        Assert.Empty(contacts);
    }

    [Fact]
    public async Task SetServiceContacts_ServiceNotFound_Returns404()
    {
        var contactId = await CreateContact();
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/services/{Guid.NewGuid()}/contacts",
            new { ContactIds = new[] { contactId } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
