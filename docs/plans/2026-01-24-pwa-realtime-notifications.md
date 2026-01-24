# PWA + Real-Time Notifications Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make mkat installable as a PWA that delivers native OS notifications for alerts via SSE (when open) and Web Push (when closed).

**Architecture:** Hybrid real-time notification system. SSE provides instant UI updates and notifications while the app is open. Web Push delivers notifications via service worker even when the app window is closed. Both are additive — existing REST API and polling remain unchanged.

**Tech Stack:** ASP.NET Core SSE endpoint, System.Threading.Channels for in-memory broadcast, WebPush NuGet for push messages, VAPID keys for self-hosted auth, Vite PWA manifest, vanilla JS service worker.

---

## Task 1: PushSubscription Domain Entity

**Files:**
- Create: `src/Mkat.Domain/Entities/PushSubscription.cs`
- Test: `tests/Mkat.Domain.Tests/Entities/PushSubscriptionTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Domain.Tests/Entities/PushSubscriptionTests.cs
using Mkat.Domain.Entities;
using Xunit;

namespace Mkat.Domain.Tests.Entities;

public class PushSubscriptionTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sub = new PushSubscription();

        Assert.NotEqual(Guid.Empty, sub.Id);
        Assert.True(sub.CreatedAtUtc <= DateTime.UtcNow);
        Assert.True(sub.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var sub = new PushSubscription
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
            P256dhKey = "BNcRd...",
            AuthKey = "tBHI..."
        };

        Assert.Equal("https://fcm.googleapis.com/fcm/send/abc123", sub.Endpoint);
        Assert.Equal("BNcRd...", sub.P256dhKey);
        Assert.Equal("tBHI...", sub.AuthKey);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Domain.Tests --filter "PushSubscriptionTests" -v n`
Expected: FAIL — `PushSubscription` type does not exist.

**Step 3: Write minimal implementation**

```csharp
// src/Mkat.Domain/Entities/PushSubscription.cs
namespace Mkat.Domain.Entities;

public class PushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Endpoint { get; set; } = string.Empty;
    public string P256dhKey { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Domain.Tests --filter "PushSubscriptionTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Domain/Entities/PushSubscription.cs tests/Mkat.Domain.Tests/Entities/PushSubscriptionTests.cs
git commit -m "feat: add PushSubscription domain entity"
```

---

## Task 2: IEventBroadcaster Interface

**Files:**
- Create: `src/Mkat.Application/Interfaces/IEventBroadcaster.cs`
- Create: `src/Mkat.Application/DTOs/ServerEvent.cs`
- Test: `tests/Mkat.Application.Tests/Interfaces/InterfaceDefinitionTests.cs` (add to existing)

**Step 1: Write the failing test**

Add to `tests/Mkat.Application.Tests/Interfaces/InterfaceDefinitionTests.cs`:

```csharp
[Fact]
public void IEventBroadcaster_HasExpectedMethods()
{
    var type = typeof(IEventBroadcaster);
    Assert.NotNull(type);

    var broadcast = type.GetMethod("BroadcastAsync");
    Assert.NotNull(broadcast);
    Assert.Equal(typeof(Task), broadcast!.ReturnType);

    var subscribe = type.GetMethod("Subscribe");
    Assert.NotNull(subscribe);
}

[Fact]
public void ServerEvent_HasExpectedProperties()
{
    var evt = new ServerEvent
    {
        Type = "alert_created",
        Payload = "{}"
    };
    Assert.Equal("alert_created", evt.Type);
    Assert.Equal("{}", evt.Payload);
    Assert.True(evt.Timestamp <= DateTime.UtcNow);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Application.Tests --filter "IEventBroadcaster_HasExpectedMethods|ServerEvent_HasExpectedProperties" -v n`
Expected: FAIL — types do not exist.

**Step 3: Write minimal implementation**

```csharp
// src/Mkat.Application/DTOs/ServerEvent.cs
namespace Mkat.Application.DTOs;

public class ServerEvent
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

```csharp
// src/Mkat.Application/Interfaces/IEventBroadcaster.cs
using Mkat.Application.DTOs;

namespace Mkat.Application.Interfaces;

public interface IEventBroadcaster
{
    Task BroadcastAsync(ServerEvent serverEvent, CancellationToken ct = default);
    IAsyncEnumerable<ServerEvent> Subscribe(CancellationToken ct = default);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Application.Tests --filter "IEventBroadcaster_HasExpectedMethods|ServerEvent_HasExpectedProperties" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Application/DTOs/ServerEvent.cs src/Mkat.Application/Interfaces/IEventBroadcaster.cs tests/Mkat.Application.Tests/Interfaces/InterfaceDefinitionTests.cs
git commit -m "feat: add IEventBroadcaster interface and ServerEvent DTO"
```

---

## Task 3: EventBroadcaster Implementation

**Files:**
- Create: `src/Mkat.Infrastructure/Services/EventBroadcaster.cs`
- Test: `tests/Mkat.Api.Tests/Services/EventBroadcasterTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/Services/EventBroadcasterTests.cs
using Mkat.Application.DTOs;
using Mkat.Infrastructure.Services;
using Xunit;

namespace Mkat.Api.Tests.Services;

public class EventBroadcasterTests
{
    [Fact]
    public async Task Broadcast_DeliversToSubscriber()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var received = new List<ServerEvent>();

        // Subscribe in background
        var readTask = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received.Add(evt);
                if (received.Count >= 1) break;
            }
        });

        // Give subscriber time to register
        await Task.Delay(50);

        await broadcaster.BroadcastAsync(new ServerEvent { Type = "test", Payload = "hello" });

        await Task.WhenAny(readTask, Task.Delay(1000));
        cts.Cancel();

        Assert.Single(received);
        Assert.Equal("test", received[0].Type);
        Assert.Equal("hello", received[0].Payload);
    }

    [Fact]
    public async Task Broadcast_DeliversToMultipleSubscribers()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var received1 = new List<ServerEvent>();
        var received2 = new List<ServerEvent>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received1.Add(evt);
                if (received1.Count >= 1) break;
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received2.Add(evt);
                if (received2.Count >= 1) break;
            }
        });

        await Task.Delay(50);

        await broadcaster.BroadcastAsync(new ServerEvent { Type = "alert", Payload = "{}" });

        await Task.WhenAny(Task.WhenAll(task1, task2), Task.Delay(1000));
        cts.Cancel();

        Assert.Single(received1);
        Assert.Single(received2);
    }

    [Fact]
    public async Task Subscribe_CompletesOnCancellation()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var count = 0;

        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in broadcaster.Subscribe(cts.Token))
            {
                count++;
            }
        });

        await Task.Delay(50);
        cts.Cancel();

        await Task.WhenAny(readTask, Task.Delay(1000));
        Assert.Equal(0, count);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "EventBroadcasterTests" -v n`
Expected: FAIL — `EventBroadcaster` type does not exist.

**Step 3: Write minimal implementation**

```csharp
// src/Mkat.Infrastructure/Services/EventBroadcaster.cs
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;

namespace Mkat.Infrastructure.Services;

public class EventBroadcaster : IEventBroadcaster
{
    private readonly List<Channel<ServerEvent>> _subscribers = new();
    private readonly Lock _lock = new();

    public Task BroadcastAsync(ServerEvent serverEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            // Remove completed channels and write to active ones
            _subscribers.RemoveAll(ch => ch.Writer.TryComplete());
            foreach (var channel in _subscribers)
            {
                channel.Writer.TryWrite(serverEvent);
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ServerEvent> Subscribe(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateBounded<ServerEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                yield return evt;
            }
        }
        finally
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "EventBroadcasterTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Infrastructure/Services/EventBroadcaster.cs tests/Mkat.Api.Tests/Services/EventBroadcasterTests.cs
git commit -m "feat: add EventBroadcaster with Channel-based pub/sub"
```

---

## Task 4: SSE Events Controller

**Files:**
- Create: `src/Mkat.Api/Controllers/EventsController.cs`
- Test: `tests/Mkat.Api.Tests/Controllers/EventsControllerTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/Controllers/EventsControllerTests.cs
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
        await Task.Delay(100); // Let the stream establish
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
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "EventsControllerTests" -v n`
Expected: FAIL — controller/endpoint does not exist.

**Step 3: Write minimal implementation**

```csharp
// src/Mkat.Api/Controllers/EventsController.cs
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventBroadcaster _broadcaster;

    public EventsController(IEventBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        await Response.Body.FlushAsync(ct);

        await foreach (var evt in _broadcaster.Subscribe(ct))
        {
            var payload = $"event: {evt.Type}\ndata: {evt.Payload}\n\n";
            await Response.WriteAsync(payload, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
```

Also register `EventBroadcaster` as singleton in `Program.cs` — add after line 42 (after `AddSingleton<IPairingService>`):

```csharp
builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();
```

Add the using at the top of `Program.cs`:
```csharp
using Mkat.Infrastructure.Services;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "EventsControllerTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Api/Controllers/EventsController.cs tests/Mkat.Api.Tests/Controllers/EventsControllerTests.cs src/Mkat.Api/Program.cs
git commit -m "feat: add SSE events stream endpoint"
```

---

## Task 5: Wire AlertDispatchWorker to EventBroadcaster

**Files:**
- Modify: `src/Mkat.Infrastructure/Workers/AlertDispatchWorker.cs`
- Modify: `tests/Mkat.Api.Tests/Workers/AlertDispatchWorkerTests.cs`

**Step 1: Write the failing test**

Add to `tests/Mkat.Api.Tests/Workers/AlertDispatchWorkerTests.cs`:

```csharp
[Fact]
public async Task DispatchPendingAlerts_BroadcastsEventOnSuccess()
{
    var broadcaster = new EventBroadcaster();
    var received = new List<ServerEvent>();
    var cts = new CancellationTokenSource();

    var readTask = Task.Run(async () =>
    {
        await foreach (var evt in broadcaster.Subscribe(cts.Token))
        {
            received.Add(evt);
            if (received.Count >= 1) break;
        }
    });

    await Task.Delay(50);

    // Set up the worker with the broadcaster
    // (Adjust constructor/DI as needed based on existing test patterns)
    // ... dispatch an alert ...

    await Task.WhenAny(readTask, Task.Delay(2000));
    cts.Cancel();

    Assert.Single(received);
    Assert.Equal("alert_dispatched", received[0].Type);
}
```

> **Note to implementer:** Look at the existing `AlertDispatchWorkerTests.cs` patterns for how the worker is constructed in tests. The broadcaster is injected via `IServiceProvider`. The test should set up a mock/real alert, run `DispatchPendingAlertsAsync`, and verify the broadcaster received the event.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "DispatchPendingAlerts_BroadcastsEventOnSuccess" -v n`
Expected: FAIL — worker doesn't broadcast events yet.

**Step 3: Modify AlertDispatchWorker**

Add `IEventBroadcaster` resolution and broadcast call after successful dispatch. In `DispatchPendingAlertsAsync`, after `await dispatcher.DispatchAsync(alert, ct);` succeeds, add:

```csharp
var broadcaster = scope.ServiceProvider.GetRequiredService<IEventBroadcaster>();
await broadcaster.BroadcastAsync(new ServerEvent
{
    Type = "alert_dispatched",
    Payload = JsonSerializer.Serialize(new
    {
        alertId = alert.Id,
        serviceId = alert.ServiceId,
        message = alert.Message,
        severity = alert.Severity.ToString(),
        type = alert.Type.ToString()
    })
}, ct);
```

Add `using System.Text.Json;` and `using Mkat.Application.DTOs;` to the file.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "AlertDispatchWorkerTests" -v n`
Expected: ALL PASS

**Step 5: Commit**

```bash
git add src/Mkat.Infrastructure/Workers/AlertDispatchWorker.cs tests/Mkat.Api.Tests/Workers/AlertDispatchWorkerTests.cs
git commit -m "feat: broadcast SSE events on alert dispatch"
```

---

## Task 6: IPushSubscriptionRepository and EF Core Setup

**Files:**
- Create: `src/Mkat.Application/Interfaces/IPushSubscriptionRepository.cs`
- Create: `src/Mkat.Infrastructure/Repositories/PushSubscriptionRepository.cs`
- Modify: `src/Mkat.Infrastructure/Data/MkatDbContext.cs` (add DbSet)
- Test: `tests/Mkat.Api.Tests/Repositories/PushSubscriptionRepositoryTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/Repositories/PushSubscriptionRepositoryTests.cs
using Microsoft.EntityFrameworkCore;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Repositories;

public class PushSubscriptionRepositoryTests : IDisposable
{
    private readonly MkatDbContext _db;
    private readonly PushSubscriptionRepository _repo;

    public PushSubscriptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _db = new MkatDbContext(options);
        _repo = new PushSubscriptionRepository(_db);
    }

    [Fact]
    public async Task AddAsync_PersistsSubscription()
    {
        var sub = new PushSubscription
        {
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "key1",
            AuthKey = "auth1"
        };

        await _repo.AddAsync(sub);
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("https://push.example.com/sub1", all[0].Endpoint);
    }

    [Fact]
    public async Task RemoveByEndpointAsync_RemovesMatchingSubscription()
    {
        _db.Set<PushSubscription>().Add(new PushSubscription
        {
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "key1",
            AuthKey = "auth1"
        });
        await _db.SaveChangesAsync();

        await _repo.RemoveByEndpointAsync("https://push.example.com/sub1");
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSubscriptions()
    {
        _db.Set<PushSubscription>().AddRange(
            new PushSubscription { Endpoint = "https://a.com", P256dhKey = "k1", AuthKey = "a1" },
            new PushSubscription { Endpoint = "https://b.com", P256dhKey = "k2", AuthKey = "a2" }
        );
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    public void Dispose() => _db.Dispose();
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "PushSubscriptionRepositoryTests" -v n`
Expected: FAIL — types do not exist.

**Step 3: Write implementations**

```csharp
// src/Mkat.Application/Interfaces/IPushSubscriptionRepository.cs
using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PushSubscription subscription, CancellationToken ct = default);
    Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default);
}
```

```csharp
// src/Mkat.Infrastructure/Repositories/PushSubscriptionRepository.cs
using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly MkatDbContext _db;

    public PushSubscriptionRepository(MkatDbContext db) => _db = db;

    public async Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct = default)
        => await _db.PushSubscriptions.ToListAsync(ct);

    public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default)
        => await _db.PushSubscriptions.AddAsync(subscription, ct);

    public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default)
    {
        var sub = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);
        if (sub != null) _db.PushSubscriptions.Remove(sub);
    }
}
```

Add to `MkatDbContext.cs` (after the `ServiceContacts` DbSet):
```csharp
public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
```

Add to `OnModelCreating` in `MkatDbContext.cs`:
```csharp
modelBuilder.Entity<PushSubscription>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(2000);
    entity.Property(e => e.P256dhKey).IsRequired().HasMaxLength(500);
    entity.Property(e => e.AuthKey).IsRequired().HasMaxLength(500);
    entity.HasIndex(e => e.Endpoint).IsUnique();
});
```

Register in `Program.cs`:
```csharp
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "PushSubscriptionRepositoryTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Application/Interfaces/IPushSubscriptionRepository.cs src/Mkat.Infrastructure/Repositories/PushSubscriptionRepository.cs src/Mkat.Infrastructure/Data/MkatDbContext.cs tests/Mkat.Api.Tests/Repositories/PushSubscriptionRepositoryTests.cs src/Mkat.Api/Program.cs
git commit -m "feat: add PushSubscription repository and EF Core mapping"
```

---

## Task 7: WebPushChannel (INotificationChannel implementation)

**Files:**
- Create: `src/Mkat.Infrastructure/Channels/WebPushChannel.cs`
- Create: `src/Mkat.Infrastructure/Channels/VapidOptions.cs`
- Test: `tests/Mkat.Api.Tests/Channels/WebPushChannelTests.cs`

**Step 1: Add NuGet package**

```bash
dotnet add src/Mkat.Infrastructure/Mkat.Infrastructure.csproj package WebPush
```

**Step 2: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/Channels/WebPushChannelTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Channels;
using Mkat.Infrastructure.Data;
using Mkat.Infrastructure.Repositories;
using Xunit;

namespace Mkat.Api.Tests.Channels;

public class WebPushChannelTests : IDisposable
{
    private readonly MkatDbContext _db;
    private readonly PushSubscriptionRepository _repo;
    private readonly Mock<ILogger<WebPushChannel>> _logger;

    public WebPushChannelTests()
    {
        var options = new DbContextOptionsBuilder<MkatDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _db = new MkatDbContext(options);
        _repo = new PushSubscriptionRepository(_db);
        _logger = new Mock<ILogger<WebPushChannel>>();
    }

    [Fact]
    public void ChannelType_ReturnsWebPush()
    {
        var vapidOptions = Options.Create(new VapidOptions());
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.Equal("WebPush", channel.ChannelType);
    }

    [Fact]
    public void IsEnabled_TrueWhenVapidConfigured()
    {
        var vapidOptions = Options.Create(new VapidOptions
        {
            PublicKey = "BNcRd...",
            PrivateKey = "a1b2...",
            Subject = "mailto:admin@example.com"
        });
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.True(channel.IsEnabled);
    }

    [Fact]
    public void IsEnabled_FalseWhenVapidNotConfigured()
    {
        var vapidOptions = Options.Create(new VapidOptions());
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        Assert.False(channel.IsEnabled);
    }

    [Fact]
    public async Task SendAlertAsync_ReturnsTrueWhenNoSubscriptions()
    {
        var vapidOptions = Options.Create(new VapidOptions
        {
            PublicKey = "BNcRd...",
            PrivateKey = "a1b2...",
            Subject = "mailto:admin@example.com"
        });
        var channel = new WebPushChannel(_repo, vapidOptions, _logger.Object);

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Message = "Service is down",
            Severity = Severity.Critical,
            Type = AlertType.Down
        };
        var service = new Service { Id = alert.ServiceId, Name = "TestService" };

        var result = await channel.SendAlertAsync(alert, service);
        Assert.True(result);
    }

    public void Dispose() => _db.Dispose();
}
```

**Step 3: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "WebPushChannelTests" -v n`
Expected: FAIL — `WebPushChannel` and `VapidOptions` do not exist.

**Step 4: Write implementations**

```csharp
// src/Mkat.Infrastructure/Channels/VapidOptions.cs
namespace Mkat.Infrastructure.Channels;

public class VapidOptions
{
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
```

```csharp
// src/Mkat.Infrastructure/Channels/WebPushChannel.cs
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using WebPush;
using PushSubscription = Mkat.Domain.Entities.PushSubscription;

namespace Mkat.Infrastructure.Channels;

public class WebPushChannel : INotificationChannel
{
    private readonly IPushSubscriptionRepository _subscriptionRepo;
    private readonly VapidOptions _vapidOptions;
    private readonly ILogger<WebPushChannel> _logger;

    public WebPushChannel(
        IPushSubscriptionRepository subscriptionRepo,
        IOptions<VapidOptions> vapidOptions,
        ILogger<WebPushChannel> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _vapidOptions = vapidOptions.Value;
        _logger = logger;
    }

    public string ChannelType => "WebPush";

    public bool IsEnabled => !string.IsNullOrEmpty(_vapidOptions.PublicKey)
                          && !string.IsNullOrEmpty(_vapidOptions.PrivateKey)
                          && !string.IsNullOrEmpty(_vapidOptions.Subject);

    public async Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default)
    {
        if (!IsEnabled) return false;

        var subscriptions = await _subscriptionRepo.GetAllAsync(ct);
        if (!subscriptions.Any()) return true;

        var payload = JsonSerializer.Serialize(new
        {
            title = $"mkat: {service.Name}",
            body = alert.Message,
            url = $"/services/{service.Id}"
        });

        var vapidDetails = new VapidDetails(_vapidOptions.Subject, _vapidOptions.PublicKey, _vapidOptions.PrivateKey);
        var webPushClient = new WebPushClient();

        var allSucceeded = true;
        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dhKey, sub.AuthKey);
                await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation("Removing expired push subscription {Endpoint}", sub.Endpoint);
                await _subscriptionRepo.RemoveByEndpointAsync(sub.Endpoint, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push to {Endpoint}", sub.Endpoint);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken ct = default)
        => Task.FromResult(IsEnabled);
}
```

**Step 5: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "WebPushChannelTests" -v n`
Expected: PASS

**Step 6: Commit**

```bash
git add src/Mkat.Infrastructure/Channels/WebPushChannel.cs src/Mkat.Infrastructure/Channels/VapidOptions.cs tests/Mkat.Api.Tests/Channels/WebPushChannelTests.cs src/Mkat.Infrastructure/Mkat.Infrastructure.csproj
git commit -m "feat: add WebPushChannel notification implementation"
```

---

## Task 8: Push Subscription API Controller

**Files:**
- Create: `src/Mkat.Api/Controllers/PushController.cs`
- Create: `src/Mkat.Application/DTOs/PushSubscriptionRequest.cs`
- Test: `tests/Mkat.Api.Tests/Controllers/PushControllerTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/Controllers/PushControllerTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;
using Xunit;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class PushControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PushControllerTests()
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

        _client = _factory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:test123"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    [Fact]
    public async Task Subscribe_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/sub1",
            keys = new { p256dh = "BNcRd...", auth = "tBHI..." }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Subscribe_PersistsSubscription()
    {
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/sub2",
            keys = new { p256dh = "key2", auth = "auth2" }
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == "https://push.example.com/sub2");
        Assert.NotNull(sub);
        Assert.Equal("key2", sub.P256dhKey);
    }

    [Fact]
    public async Task Subscribe_DuplicateEndpoint_ReturnsOk()
    {
        var body = new { endpoint = "https://push.example.com/dup", keys = new { p256dh = "k", auth = "a" } };
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", body);
        var response = await _client.PostAsJsonAsync("/api/v1/push/subscribe", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_RemovesSubscription()
    {
        await _client.PostAsJsonAsync("/api/v1/push/subscribe", new
        {
            endpoint = "https://push.example.com/remove-me",
            keys = new { p256dh = "k", auth = "a" }
        });

        var response = await _client.PostAsJsonAsync("/api/v1/push/unsubscribe", new
        {
            endpoint = "https://push.example.com/remove-me"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == "https://push.example.com/remove-me");
        Assert.Null(sub);
    }

    [Fact]
    public async Task GetVapidPublicKey_ReturnsKey()
    {
        Environment.SetEnvironmentVariable("MKAT_VAPID_PUBLIC_KEY", "test-public-key");
        var response = await _client.GetAsync("/api/v1/push/vapid-public-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("publicKey"));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Mkat.Api.Tests --filter "PushControllerTests" -v n`
Expected: FAIL — controller/endpoints do not exist.

**Step 3: Write implementations**

```csharp
// src/Mkat.Application/DTOs/PushSubscriptionRequest.cs
namespace Mkat.Application.DTOs;

public record PushSubscriptionRequest
{
    public string Endpoint { get; init; } = string.Empty;
    public PushSubscriptionKeys Keys { get; init; } = new();
}

public record PushSubscriptionKeys
{
    public string P256dh { get; init; } = string.Empty;
    public string Auth { get; init; } = string.Empty;
}

public record PushUnsubscribeRequest
{
    public string Endpoint { get; init; } = string.Empty;
}
```

```csharp
// src/Mkat.Api/Controllers/PushController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Channels;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/push")]
public class PushController : ControllerBase
{
    private readonly IPushSubscriptionRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly VapidOptions _vapidOptions;

    public PushController(
        IPushSubscriptionRepository repo,
        IUnitOfWork unitOfWork,
        IOptions<VapidOptions> vapidOptions)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _vapidOptions = vapidOptions.Value;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        var existing = (await _repo.GetAllAsync(ct))
            .FirstOrDefault(s => s.Endpoint == request.Endpoint);

        if (existing != null) return Ok();

        await _repo.AddAsync(new PushSubscription
        {
            Endpoint = request.Endpoint,
            P256dhKey = request.Keys.P256dh,
            AuthKey = request.Keys.Auth
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return Created();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken ct)
    {
        await _repo.RemoveByEndpointAsync(request.Endpoint, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        return Ok(new { publicKey = _vapidOptions.PublicKey });
    }
}
```

Register `VapidOptions` and `WebPushChannel` in `Program.cs`:
```csharp
builder.Services.Configure<VapidOptions>(options =>
{
    options.PublicKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PUBLIC_KEY") ?? "";
    options.PrivateKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PRIVATE_KEY") ?? "";
    options.Subject = Environment.GetEnvironmentVariable("MKAT_VAPID_SUBJECT") ?? "mailto:admin@localhost";
});

builder.Services.AddSingleton<INotificationChannel, WebPushChannel>();
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Mkat.Api.Tests --filter "PushControllerTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Mkat.Api/Controllers/PushController.cs src/Mkat.Application/DTOs/PushSubscriptionRequest.cs tests/Mkat.Api.Tests/Controllers/PushControllerTests.cs src/Mkat.Api/Program.cs
git commit -m "feat: add push subscription API endpoints"
```

---

## Task 9: PWA Manifest and Icons

**Files:**
- Create: `src/mkat-ui/public/manifest.json`
- Create: `src/mkat-ui/public/icons/icon-192.png` (placeholder)
- Create: `src/mkat-ui/public/icons/icon-512.png` (placeholder)
- Modify: `src/mkat-ui/index.html` (add manifest link, theme-color)

**Step 1: Create manifest.json**

```json
{
  "name": "mkat",
  "short_name": "mkat",
  "description": "Service monitoring and healthcheck dashboard",
  "start_url": "./",
  "display": "standalone",
  "background_color": "#0f172a",
  "theme_color": "#0f172a",
  "icons": [
    {
      "src": "icons/icon-192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "icons/icon-512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any maskable"
    }
  ]
}
```

**Step 2: Generate placeholder icons**

Use a simple SVG-to-PNG approach or create minimal PNG icons. For now, generate simple solid-color icons (the implementer can replace with proper branding later):

```bash
# Using ImageMagick if available, or just create placeholder files
convert -size 192x192 xc:#0f172a src/mkat-ui/public/icons/icon-192.png 2>/dev/null || \
  echo "Create 192x192 PNG icon manually at src/mkat-ui/public/icons/icon-192.png"
convert -size 512x512 xc:#0f172a src/mkat-ui/public/icons/icon-512.png 2>/dev/null || \
  echo "Create 512x512 PNG icon manually at src/mkat-ui/public/icons/icon-512.png"
```

> **Note:** If ImageMagick isn't available, create minimal valid PNG files programmatically or use any 192x192 and 512x512 PNG images as placeholders.

**Step 3: Update index.html**

Add to `<head>` in `src/mkat-ui/index.html`:
```html
<link rel="manifest" href="./manifest.json" />
<meta name="theme-color" content="#0f172a" />
<link rel="apple-touch-icon" href="./icons/icon-192.png" />
```

**Step 4: Verify build**

```bash
cd src/mkat-ui && npm run build
ls ../Mkat.Api/wwwroot/manifest.json
ls ../Mkat.Api/wwwroot/icons/
```

Expected: Files exist in wwwroot output.

**Step 5: Commit**

```bash
git add src/mkat-ui/public/manifest.json src/mkat-ui/public/icons/ src/mkat-ui/index.html
git commit -m "feat: add PWA manifest and app icons"
```

---

## Task 10: Service Worker (Push + Notification Click)

**Files:**
- Create: `src/mkat-ui/public/sw.js`

**Step 1: Create service worker**

```javascript
// src/mkat-ui/public/sw.js

// Push notification received (app may be closed)
self.addEventListener('push', (event) => {
  if (!event.data) return;

  const data = event.data.json();
  const options = {
    body: data.body || 'New alert',
    icon: './icons/icon-192.png',
    badge: './icons/icon-192.png',
    tag: data.tag || 'mkat-alert',
    data: { url: data.url || './' },
    requireInteraction: true
  };

  event.waitUntil(
    self.registration.showNotification(data.title || 'mkat', options)
  );
});

// Notification clicked — focus or open app
self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const targetUrl = event.notification.data?.url || './';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
      // Focus existing window if available
      for (const client of windowClients) {
        if (client.url.includes(self.location.origin) && 'focus' in client) {
          return client.focus();
        }
      }
      // Otherwise open new window
      return clients.openWindow(targetUrl);
    })
  );
});
```

**Step 2: Verify file is served**

```bash
cd src/mkat-ui && npm run build
cat ../Mkat.Api/wwwroot/sw.js | head -5
```

Expected: Service worker file exists in wwwroot output.

**Step 3: Commit**

```bash
git add src/mkat-ui/public/sw.js
git commit -m "feat: add service worker for push notifications"
```

---

## Task 11: Frontend — Service Worker Registration and Push Subscription

**Files:**
- Create: `src/mkat-ui/src/notifications.ts`
- Modify: `src/mkat-ui/src/App.tsx`

**Step 1: Create notifications module**

```typescript
// src/mkat-ui/src/notifications.ts
import { getBasePath } from './config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) return '';
  return `Basic ${credentials}`;
}

export async function initNotifications(): Promise<void> {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.warn('Push notifications not supported');
    return;
  }

  try {
    const registration = await navigator.serviceWorker.register('./sw.js');
    console.log('Service worker registered');

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      console.warn('Notification permission denied');
      return;
    }

    await subscribeToPush(registration);
  } catch (err) {
    console.error('Failed to init notifications:', err);
  }
}

async function subscribeToPush(registration: ServiceWorkerRegistration): Promise<void> {
  // Get VAPID public key from server
  const authHeader = getAuthHeader();
  if (!authHeader) return;

  const keyResponse = await fetch(`${getApiBase()}/push/vapid-public-key`, {
    headers: { 'Authorization': authHeader }
  });
  if (!keyResponse.ok) return;

  const { publicKey } = await keyResponse.json();
  if (!publicKey) return;

  // Check for existing subscription
  let subscription = await registration.pushManager.getSubscription();

  if (!subscription) {
    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey)
    });
  }

  // Send subscription to backend
  const sub = subscription.toJSON();
  await fetch(`${getApiBase()}/push/subscribe`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': authHeader
    },
    body: JSON.stringify({
      endpoint: sub.endpoint,
      keys: {
        p256dh: sub.keys?.p256dh || '',
        auth: sub.keys?.auth || ''
      }
    })
  });
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
```

**Step 2: Create SSE connection module**

```typescript
// src/mkat-ui/src/sse.ts
import { getBasePath } from './config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) return '';
  return `Basic ${credentials}`;
}

export function connectSSE(onEvent: (type: string, data: unknown) => void): () => void {
  const authHeader = getAuthHeader();
  if (!authHeader) return () => {};

  // EventSource doesn't support custom headers, use URL-based auth or fetch-based SSE
  const controller = new AbortController();

  (async () => {
    try {
      const response = await fetch(`${getApiBase()}/events/stream`, {
        headers: { 'Authorization': authHeader },
        signal: controller.signal
      });

      if (!response.ok || !response.body) return;

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let currentEvent = '';
      let currentData = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('event: ')) {
            currentEvent = line.slice(7).trim();
          } else if (line.startsWith('data: ')) {
            currentData = line.slice(6);
          } else if (line === '') {
            if (currentEvent && currentData) {
              try {
                const parsed = JSON.parse(currentData);
                onEvent(currentEvent, parsed);
              } catch {
                onEvent(currentEvent, currentData);
              }
            }
            currentEvent = '';
            currentData = '';
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name !== 'AbortError') {
        console.error('SSE connection error:', err);
        // Reconnect after 5s
        setTimeout(() => connectSSE(onEvent), 5000);
      }
    }
  })();

  return () => controller.abort();
}
```

**Step 3: Integrate into App.tsx**

Add to `src/mkat-ui/src/App.tsx` — import and call `initNotifications` and `connectSSE` on mount (inside the App component, after auth check). The SSE connection should:
- Show a browser `Notification` when an `alert_dispatched` event arrives
- Invalidate TanStack Query cache for alerts

```typescript
import { useEffect } from 'react';
import { initNotifications } from './notifications';
import { connectSSE } from './sse';

// Inside the App component (or a layout component that renders after auth):
useEffect(() => {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) return;

  initNotifications();

  const disconnect = connectSSE((type, data) => {
    if (type === 'alert_dispatched' && Notification.permission === 'granted') {
      const alertData = data as { message?: string; serviceId?: string };
      new Notification('mkat Alert', {
        body: alertData.message || 'New alert',
        icon: './icons/icon-192.png'
      });
    }
    // Invalidate queries to refresh UI
    queryClient.invalidateQueries({ queryKey: ['alerts'] });
    queryClient.invalidateQueries({ queryKey: ['services'] });
  });

  return () => disconnect();
}, []);
```

> **Note to implementer:** Check the existing App.tsx structure to see where auth state is managed and place the `useEffect` appropriately (after login, not on the login page). The `queryClient` reference needs to be accessible.

**Step 4: Verify build**

```bash
cd src/mkat-ui && npm run build
```

Expected: Build succeeds without TypeScript errors.

**Step 5: Commit**

```bash
git add src/mkat-ui/src/notifications.ts src/mkat-ui/src/sse.ts src/mkat-ui/src/App.tsx
git commit -m "feat: add frontend push subscription and SSE connection"
```

---

## Task 12: Register WebPushChannel and VAPID in Program.cs

**Files:**
- Modify: `src/Mkat.Api/Program.cs`

This task ensures all DI registrations from previous tasks are consolidated and the VAPID options + WebPushChannel are properly wired.

**Step 1: Verify all registrations exist in Program.cs**

The following should be registered (some were added in earlier tasks):
```csharp
// EventBroadcaster (Task 4)
builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();

// PushSubscription repository (Task 6)
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

// VAPID options (Task 8)
builder.Services.Configure<VapidOptions>(options =>
{
    options.PublicKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PUBLIC_KEY") ?? "";
    options.PrivateKey = Environment.GetEnvironmentVariable("MKAT_VAPID_PRIVATE_KEY") ?? "";
    options.Subject = Environment.GetEnvironmentVariable("MKAT_VAPID_SUBJECT") ?? "mailto:admin@localhost";
});

// WebPushChannel as additional INotificationChannel (Task 8)
builder.Services.AddSingleton<INotificationChannel, WebPushChannel>();
```

**Step 2: Run full test suite**

```bash
dotnet test
```

Expected: ALL PASS

**Step 3: Commit (if any consolidation was needed)**

```bash
git add src/Mkat.Api/Program.cs
git commit -m "chore: consolidate PWA and push notification DI registrations"
```

---

## Task 13: EF Core Migration

**Files:**
- Create: `src/Mkat.Infrastructure/Migrations/XXXXXXXX_AddPushSubscriptions.cs` (auto-generated)

**Step 1: Generate migration**

```bash
dotnet ef migrations add AddPushSubscriptions -p src/Mkat.Infrastructure -s src/Mkat.Api
```

**Step 2: Verify migration**

```bash
dotnet ef database update -p src/Mkat.Infrastructure -s src/Mkat.Api
```

Expected: Migration applies cleanly.

**Step 3: Run full test suite**

```bash
dotnet test
```

Expected: ALL PASS

**Step 4: Commit**

```bash
git add src/Mkat.Infrastructure/Migrations/
git commit -m "feat: add PushSubscriptions migration"
```

---

## Task 14: Vite Dev Proxy for SSE and Push Endpoints

**Files:**
- Modify: `src/mkat-ui/vite.config.ts`

**Step 1: Add proxy entries**

Add to the `proxy` config in `vite.config.ts`:
```typescript
'/api/v1/events': 'http://localhost:8080',
'/api/v1/push': 'http://localhost:8080',
```

> **Note:** The existing `/api` proxy may already cover these. Check if the wildcard catch works. If not, add explicit entries for SSE (which needs special handling for streaming).

For SSE specifically, the proxy may need `ws: false` and streaming support:
```typescript
'/api/v1/events/stream': {
  target: 'http://localhost:8080',
  headers: { 'Connection': 'keep-alive' }
},
```

**Step 2: Verify dev server**

```bash
cd src/mkat-ui && npm run dev
# In another terminal, verify proxy works:
# curl -N -H "Authorization: Basic YWRtaW46dGVzdDEyMw==" http://localhost:5173/api/v1/events/stream
```

**Step 3: Commit**

```bash
git add src/mkat-ui/vite.config.ts
git commit -m "chore: add SSE and push proxy entries to Vite dev config"
```

---

## Task 15: Update Documentation

**Files:**
- Modify: `docs/changelog.md`
- Modify: `CLAUDE.md` (add VAPID env vars to table)

**Step 1: Add changelog entry**

Add to `docs/changelog.md`:
```markdown
## [Unreleased]

### Added
- PWA support: manifest.json, service worker, installable app
- SSE real-time event stream (`GET /api/v1/events/stream`)
- Web Push notifications for alerts when app is closed
- Push subscription management (`POST /api/v1/push/subscribe`, `POST /api/v1/push/unsubscribe`)
- VAPID key configuration for self-hosted push notifications
```

**Step 2: Update CLAUDE.md env vars table**

Add to the Environment Variables table:
```markdown
| MKAT_VAPID_PUBLIC_KEY | No | VAPID public key for Web Push |
| MKAT_VAPID_PRIVATE_KEY | No | VAPID private key for Web Push |
| MKAT_VAPID_SUBJECT | No | VAPID contact (default: mailto:admin@localhost) |
```

**Step 3: Commit**

```bash
git add docs/changelog.md CLAUDE.md
git commit -m "docs: add PWA and push notification documentation"
```

---

## Task 16: Final Verification

**Step 1: Run full test suite**

```bash
dotnet test --verbosity normal
```

Expected: ALL PASS

**Step 2: Build frontend**

```bash
cd src/mkat-ui && npm run build
```

Expected: Build succeeds.

**Step 3: Verify PWA files in output**

```bash
ls src/Mkat.Api/wwwroot/manifest.json
ls src/Mkat.Api/wwwroot/sw.js
ls src/Mkat.Api/wwwroot/icons/
```

Expected: All files present.

**Step 4: Run the app and test manually**

```bash
dotnet run --project src/Mkat.Api
# Visit http://localhost:8080
# - Check manifest loads (DevTools → Application → Manifest)
# - Check service worker registers (DevTools → Application → Service Workers)
# - Check SSE connects (DevTools → Network → EventStream)
# - Check notification permission prompt appears
```

---

## Summary of New Files

| File | Layer | Purpose |
|------|-------|---------|
| `src/Mkat.Domain/Entities/PushSubscription.cs` | Domain | Push subscription entity |
| `src/Mkat.Application/Interfaces/IEventBroadcaster.cs` | Application | SSE broadcast interface |
| `src/Mkat.Application/Interfaces/IPushSubscriptionRepository.cs` | Application | Push sub repository interface |
| `src/Mkat.Application/DTOs/ServerEvent.cs` | Application | SSE event DTO |
| `src/Mkat.Application/DTOs/PushSubscriptionRequest.cs` | Application | Push API request DTOs |
| `src/Mkat.Infrastructure/Services/EventBroadcaster.cs` | Infrastructure | Channel-based pub/sub |
| `src/Mkat.Infrastructure/Repositories/PushSubscriptionRepository.cs` | Infrastructure | EF Core push sub repo |
| `src/Mkat.Infrastructure/Channels/WebPushChannel.cs` | Infrastructure | Web Push sender |
| `src/Mkat.Infrastructure/Channels/VapidOptions.cs` | Infrastructure | VAPID config |
| `src/Mkat.Api/Controllers/EventsController.cs` | API | SSE stream endpoint |
| `src/Mkat.Api/Controllers/PushController.cs` | API | Push subscribe/unsubscribe |
| `src/mkat-ui/public/manifest.json` | Frontend | PWA manifest |
| `src/mkat-ui/public/sw.js` | Frontend | Service worker |
| `src/mkat-ui/public/icons/icon-192.png` | Frontend | App icon |
| `src/mkat-ui/public/icons/icon-512.png` | Frontend | App icon |
| `src/mkat-ui/src/notifications.ts` | Frontend | Push subscription logic |
| `src/mkat-ui/src/sse.ts` | Frontend | SSE connection + parsing |

## New Environment Variables

| Variable | Required | Purpose |
|----------|----------|---------|
| `MKAT_VAPID_PUBLIC_KEY` | No | VAPID public key (enables Web Push) |
| `MKAT_VAPID_PRIVATE_KEY` | No | VAPID private key (signs push messages) |
| `MKAT_VAPID_SUBJECT` | No | VAPID contact email (default: `mailto:admin@localhost`) |

Generate VAPID keys once:
```bash
npx web-push generate-vapid-keys
```
