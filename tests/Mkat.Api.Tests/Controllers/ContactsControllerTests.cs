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
public class ContactsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ContactsControllerTests()
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

    [Fact]
    public async Task CreateContact_ReturnsCreated()
    {
        var request = new { Name = "On-call Team" };
        var response = await _client.PostAsJsonAsync("/api/v1/contacts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("On-call Team", body.Name);
        Assert.False(body.IsDefault);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task CreateContact_EmptyName_ReturnsBadRequest()
    {
        var request = new { Name = "" };
        var response = await _client.PostAsJsonAsync("/api/v1/contacts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListContacts_ReturnsAll()
    {
        await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = "Alpha" });
        await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = "Beta" });

        var response = await _client.GetAsync("/api/v1/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contacts = await response.Content.ReadFromJsonAsync<List<ContactResponse>>(JsonOptions);
        Assert.NotNull(contacts);
        Assert.True(contacts.Count >= 2);
    }

    [Fact]
    public async Task GetContact_ReturnsWithChannels()
    {
        var createResp = await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = "Test" });
        var created = await createResp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);

        var response = await _client.GetAsync($"/api/v1/contacts/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contact = await response.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        Assert.NotNull(contact);
        Assert.Equal("Test", contact.Name);
        Assert.NotNull(contact.Channels);
    }

    [Fact]
    public async Task GetContact_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/contacts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateContact_ReturnsUpdated()
    {
        var createResp = await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = "Original" });
        var created = await createResp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/contacts/{created!.Id}", new { Name = "Updated" });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var updated = await updateResp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task DeleteContact_ReturnsNoContent()
    {
        var createResp = await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = "ToDelete" });
        var created = await createResp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);

        var response = await _client.DeleteAsync($"/api/v1/contacts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResp = await _client.GetAsync($"/api/v1/contacts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteContact_DefaultContact_ReturnsBadRequest()
    {
        // Create a default contact directly
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var contact = new Domain.Entities.Contact
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var response = await _client.DeleteAsync($"/api/v1/contacts/{contact.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
