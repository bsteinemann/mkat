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
public class ContactChannelsControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ContactChannelsControllerTests()
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

    private async Task<ContactResponse> CreateContact(string name = "Test Contact")
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/contacts", new { Name = name });
        return (await resp.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task AddChannel_ReturnsCreated()
    {
        var contact = await CreateContact();
        var request = new { Type = ChannelType.Telegram, Configuration = "{\"botToken\":\"123:ABC\",\"chatId\":\"456\"}" };

        var response = await _client.PostAsJsonAsync($"/api/v1/contacts/{contact.Id}/channels", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var channel = await response.Content.ReadFromJsonAsync<ContactChannelResponse>(JsonOptions);
        Assert.NotNull(channel);
        Assert.Equal(ChannelType.Telegram, channel.Type);
        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public async Task AddChannel_EmptyConfig_ReturnsBadRequest()
    {
        var contact = await CreateContact();
        var request = new { Type = ChannelType.Telegram, Configuration = "" };

        var response = await _client.PostAsJsonAsync($"/api/v1/contacts/{contact.Id}/channels", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddChannel_ContactNotFound_Returns404()
    {
        var request = new { Type = ChannelType.Telegram, Configuration = "{\"botToken\":\"x\",\"chatId\":\"y\"}" };
        var response = await _client.PostAsJsonAsync($"/api/v1/contacts/{Guid.NewGuid()}/channels", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateChannel_ReturnsOk()
    {
        var contact = await CreateContact();
        var addResp = await _client.PostAsJsonAsync($"/api/v1/contacts/{contact.Id}/channels",
            new { Type = ChannelType.Telegram, Configuration = "{\"botToken\":\"old\",\"chatId\":\"1\"}" });
        var channel = await addResp.Content.ReadFromJsonAsync<ContactChannelResponse>(JsonOptions);

        var updateResp = await _client.PutAsJsonAsync(
            $"/api/v1/contacts/{contact.Id}/channels/{channel!.Id}",
            new { Configuration = "{\"botToken\":\"new\",\"chatId\":\"2\"}", IsEnabled = false });

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<ContactChannelResponse>(JsonOptions);
        Assert.Contains("new", updated!.Configuration);
        Assert.False(updated.IsEnabled);
    }

    [Fact]
    public async Task UpdateChannel_NotFound_Returns404()
    {
        var contact = await CreateContact();
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/contacts/{contact.Id}/channels/{Guid.NewGuid()}",
            new { Configuration = "{}", IsEnabled = true });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChannel_ReturnsNoContent()
    {
        var contact = await CreateContact();
        var addResp = await _client.PostAsJsonAsync($"/api/v1/contacts/{contact.Id}/channels",
            new { Type = ChannelType.Telegram, Configuration = "{\"botToken\":\"x\",\"chatId\":\"y\"}" });
        var channel = await addResp.Content.ReadFromJsonAsync<ContactChannelResponse>(JsonOptions);

        var response = await _client.DeleteAsync($"/api/v1/contacts/{contact.Id}/channels/{channel!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task TestChannel_ContactOrChannelNotFound_Returns404()
    {
        var contact = await CreateContact();
        var response = await _client.PostAsync(
            $"/api/v1/contacts/{contact.Id}/channels/{Guid.NewGuid()}/test", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
