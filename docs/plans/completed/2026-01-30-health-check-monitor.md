# Health Check Monitor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add active HTTP health check monitoring so mkat can poll endpoints on a schedule and alert on failures.

**Architecture:** Add 5 dedicated columns to Monitor entity for health check config (URL, method, status codes, timeout, body regex). A new `HealthCheckWorker` background service polls due monitors, evaluates HTTP responses, and triggers state transitions via the existing `IStateService`. A new `AlertType.FailedHealthCheck` distinguishes health check failures from other alert types.

**Tech Stack:** ASP.NET Core, EF Core (SQLite), FluentValidation, IHttpClientFactory, React + TypeScript + Tailwind

**Key docs:**
- `docs/learnings.md` — Monitor naming conflicts, worker test patterns, TDD gate
- `docs/telegram_healthcheck_monitoring_prd.md` section 6.4 — PRD requirements
- `CLAUDE.md` — Coding conventions, TDD workflow

---

### Task 1: Add AlertType.FailedHealthCheck enum value

**Files:**
- Modify: `src/Mkat.Domain/Enums/AlertType.cs`
- Modify: `tests/Mkat.Domain.Tests/Enums/AlertTypeTests.cs` (if exists, otherwise `MonitorTypeTests.cs` pattern)
- Modify: `src/mkat-ui/src/api/types.ts`

**Step 1: Add enum value**

In `src/Mkat.Domain/Enums/AlertType.cs`, add:
```csharp
namespace Mkat.Domain.Enums;

public enum AlertType
{
    Failure = 0,
    Recovery = 1,
    MissedHeartbeat = 2,
    FailedHealthCheck = 3
}
```

**Step 2: Update frontend enum**

In `src/mkat-ui/src/api/types.ts`, update:
```typescript
export enum AlertType {
  Failure = 0,
  Recovery = 1,
  MissedHeartbeat = 2,
  FailedHealthCheck = 3,
}
```

**Step 3: Run tests to confirm nothing breaks**

Run: `dotnet test --verbosity quiet`
Expected: All existing tests PASS

**Step 4: Commit**

```bash
git add src/Mkat.Domain/Enums/AlertType.cs src/mkat-ui/src/api/types.ts
git commit -m "feat: add FailedHealthCheck alert type"
```

---

### Task 2: Add health check fields to Monitor entity

**Files:**
- Modify: `src/Mkat.Domain/Entities/Monitor.cs`
- Modify: `src/Mkat.Infrastructure/Data/MkatDbContext.cs`

**Step 1: Add fields to entity**

In `src/Mkat.Domain/Entities/Monitor.cs`, after the metric monitor fields block, add:
```csharp
    // Health check monitor fields
    public string? HealthCheckUrl { get; set; }
    public string? HttpMethod { get; set; }
    public string? ExpectedStatusCodes { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? BodyMatchRegex { get; set; }
```

**Step 2: Add EF configuration**

In `src/Mkat.Infrastructure/Data/MkatDbContext.cs`, inside the `modelBuilder.Entity<Monitor>` block (after the `ThresholdStrategy` config line), add:
```csharp
            entity.Property(e => e.HealthCheckUrl).HasMaxLength(2000);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.ExpectedStatusCodes).HasMaxLength(200);
            entity.Property(e => e.BodyMatchRegex).HasMaxLength(1000);
```

**Step 3: Create EF migration**

Run: `dotnet ef migrations add AddHealthCheckFields -p src/Mkat.Infrastructure -s src/Mkat.Api`
Expected: Migration file created in `src/Mkat.Infrastructure/Migrations/`

**Step 4: Run tests**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 5: Commit**

```bash
git add src/Mkat.Domain/Entities/Monitor.cs src/Mkat.Infrastructure/Data/MkatDbContext.cs src/Mkat.Infrastructure/Migrations/
git commit -m "feat: add health check fields to Monitor entity"
```

---

### Task 3: Add health check fields to DTOs

**Files:**
- Modify: `src/Mkat.Application/DTOs/CreateServiceRequest.cs`
- Modify: `src/Mkat.Application/DTOs/MonitorRequests.cs`
- Modify: `src/Mkat.Application/DTOs/ServiceResponse.cs`

**Step 1: Add to CreateMonitorRequest**

In `src/Mkat.Application/DTOs/CreateServiceRequest.cs`, after the metric fields in `CreateMonitorRequest`, add:
```csharp
    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
```

**Step 2: Add to AddMonitorRequest**

In `src/Mkat.Application/DTOs/MonitorRequests.cs`, after the metric fields in `AddMonitorRequest`, add:
```csharp
    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
```

**Step 3: Add to UpdateMonitorRequest**

In `src/Mkat.Application/DTOs/MonitorRequests.cs`, after the metric fields in `UpdateMonitorRequest`, add:
```csharp
    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
```

**Step 4: Add to MonitorResponse**

In `src/Mkat.Application/DTOs/ServiceResponse.cs`, after the metric fields in `MonitorResponse`, add:
```csharp
    // Health check monitor fields
    public string? HealthCheckUrl { get; init; }
    public string? HttpMethod { get; init; }
    public string? ExpectedStatusCodes { get; init; }
    public int? TimeoutSeconds { get; init; }
    public string? BodyMatchRegex { get; init; }
```

**Step 5: Update MapMonitorToResponse**

In `src/Mkat.Api/Controllers/ServicesController.cs`, in the `MapMonitorToResponse` method, add the health check fields to the return object:
```csharp
            HealthCheckUrl = monitor.HealthCheckUrl,
            HttpMethod = monitor.HttpMethod,
            ExpectedStatusCodes = monitor.ExpectedStatusCodes,
            TimeoutSeconds = monitor.TimeoutSeconds,
            BodyMatchRegex = monitor.BodyMatchRegex
```

**Step 6: Update monitor creation mapping in ServicesController**

Find where monitors are created from `CreateMonitorRequest` in `ServicesController.Create()` and `MonitorsController.Add()`. Add the health check field mapping:
```csharp
            HealthCheckUrl = monitorReq.HealthCheckUrl,
            HttpMethod = monitorReq.HttpMethod ?? "GET",
            ExpectedStatusCodes = monitorReq.ExpectedStatusCodes ?? "200",
            TimeoutSeconds = monitorReq.TimeoutSeconds ?? 10,
            BodyMatchRegex = monitorReq.BodyMatchRegex
```

Do the same in `MonitorsController.Add()` and `MonitorsController.Update()` for `AddMonitorRequest` / `UpdateMonitorRequest`.

**Step 7: Run tests**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 8: Commit**

```bash
git add src/Mkat.Application/DTOs/ src/Mkat.Api/Controllers/ServicesController.cs src/Mkat.Api/Controllers/MonitorsController.cs
git commit -m "feat: add health check fields to DTOs and controller mapping"
```

---

### Task 4: Add health check validation rules

**Files:**
- Modify: `src/Mkat.Application/Validators/MonitorValidators.cs`
- Modify: `src/Mkat.Application/Validators/CreateServiceValidator.cs`
- Test: `tests/Mkat.Application.Tests/Validators/MonitorValidatorTests.cs`

**Step 1: Write failing tests for health check validation**

In `tests/Mkat.Application.Tests/Validators/MonitorValidatorTests.cs`, add tests:
```csharp
    [Fact]
    public void HealthCheck_WithValidUrl_Passes()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health"
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void HealthCheck_WithoutUrl_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "HealthCheckUrl");
    }

    [Fact]
    public void HealthCheck_WithInvalidUrl_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "not-a-url"
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void HealthCheck_WithInvalidHttpMethod_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "DELETE"
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void HealthCheck_WithInvalidTimeout_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            TimeoutSeconds = 0
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void HealthCheck_WithInvalidRegex_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            BodyMatchRegex = "[invalid"
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void HealthCheck_WithInvalidStatusCodes_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60,
            HealthCheckUrl = "https://example.com/health",
            ExpectedStatusCodes = "abc,def"
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }
```

**Step 2: Run tests to verify RED**

Run: `dotnet test tests/Mkat.Application.Tests --verbosity quiet`
Expected: FAIL — HealthCheck type still blocked, validation rules missing

**Step 3: Remove HealthCheck block and add validation rules**

In `src/Mkat.Application/Validators/MonitorValidators.cs`, in `AddMonitorValidator`:
- Remove the `.Must(t => t != MonitorType.HealthCheck)` and its `.WithMessage(...)` line
- Add health check validation block after the metric block:

```csharp
        // Health check monitor validation rules
        When(x => x.Type == MonitorType.HealthCheck, () =>
        {
            RuleFor(x => x.HealthCheckUrl)
                .NotEmpty().WithMessage("HealthCheckUrl is required for health check monitors")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == "http" || uri.Scheme == "https"))
                .When(x => !string.IsNullOrEmpty(x.HealthCheckUrl))
                .WithMessage("HealthCheckUrl must be a valid HTTP or HTTPS URL");

            RuleFor(x => x.HttpMethod)
                .Must(m => new[] { "GET", "HEAD", "POST", "PUT" }.Contains(m, StringComparer.OrdinalIgnoreCase))
                .When(x => !string.IsNullOrEmpty(x.HttpMethod))
                .WithMessage("HttpMethod must be one of: GET, HEAD, POST, PUT");

            RuleFor(x => x.ExpectedStatusCodes)
                .Must(codes =>
                {
                    if (string.IsNullOrEmpty(codes)) return true;
                    return codes.Split(',').All(c =>
                        int.TryParse(c.Trim(), out var code) && code >= 100 && code <= 599);
                })
                .WithMessage("ExpectedStatusCodes must be comma-separated integers between 100 and 599");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 120)
                .When(x => x.TimeoutSeconds.HasValue)
                .WithMessage("TimeoutSeconds must be between 1 and 120");

            RuleFor(x => x.BodyMatchRegex)
                .Must(pattern =>
                {
                    try { _ = new System.Text.RegularExpressions.Regex(pattern!); return true; }
                    catch { return false; }
                })
                .When(x => !string.IsNullOrEmpty(x.BodyMatchRegex))
                .WithMessage("BodyMatchRegex must be a valid regular expression");
        });
```

Do the same for `UpdateMonitorValidator` — add the same `When` block (using the same rules).

In `src/Mkat.Application/Validators/CreateServiceValidator.cs`, in `CreateMonitorValidator`:
- Remove the `.Must(t => t != MonitorType.HealthCheck)` and its `.WithMessage(...)` line
- Add the same health check validation `When` block as above

**Step 4: Update existing test that expects HealthCheck rejection**

In `tests/Mkat.Application.Tests/Validators/MonitorValidatorTests.cs`, find the test `HealthCheckType_Fails` (or similar). Change it to verify that HealthCheck **without a URL** fails (not that the type itself is rejected):
```csharp
    [Fact]
    public void HealthCheckType_WithoutUrl_Fails()
    {
        var request = new AddMonitorRequest
        {
            Type = MonitorType.HealthCheck,
            IntervalSeconds = 60
        };
        var validator = new AddMonitorValidator();
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "HealthCheckUrl");
    }
```

Also update any similar test in `tests/Mkat.Application.Tests/Validators/CreateServiceValidatorTests.cs`.

**Step 5: Run tests to verify GREEN**

Run: `dotnet test tests/Mkat.Application.Tests --verbosity quiet`
Expected: All PASS

**Step 6: Commit**

```bash
git add src/Mkat.Application/Validators/ tests/Mkat.Application.Tests/
git commit -m "feat: add health check validation rules, remove HealthCheck type block"
```

---

### Task 5: Add repository method for due health check monitors

**Files:**
- Modify: `src/Mkat.Application/Interfaces/IMonitorRepository.cs`
- Modify: `src/Mkat.Infrastructure/Repositories/MonitorRepository.cs`

**Step 1: Add interface method**

In `src/Mkat.Application/Interfaces/IMonitorRepository.cs`, add:
```csharp
    Task<IReadOnlyList<Monitor>> GetHealthCheckMonitorsDueAsync(DateTime now, CancellationToken ct = default);
```

**Step 2: Implement repository method**

In `src/Mkat.Infrastructure/Repositories/MonitorRepository.cs`, add:
```csharp
    public async Task<IReadOnlyList<Monitor>> GetHealthCheckMonitorsDueAsync(DateTime now, CancellationToken ct = default)
    {
        return await _context.Monitors
            .Include(m => m.Service)
            .Where(m => m.Type == MonitorType.HealthCheck)
            .Where(m => m.LastCheckIn == null || m.LastCheckIn.Value.AddSeconds(m.IntervalSeconds) <= now)
            .ToListAsync(ct);
    }
```

**Step 3: Run tests**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 4: Commit**

```bash
git add src/Mkat.Application/Interfaces/IMonitorRepository.cs src/Mkat.Infrastructure/Repositories/MonitorRepository.cs
git commit -m "feat: add GetHealthCheckMonitorsDueAsync repository method"
```

---

### Task 6: Implement HealthCheckWorker

**Files:**
- Create: `src/Mkat.Infrastructure/Workers/HealthCheckWorker.cs`
- Modify: `src/Mkat.Api/Program.cs`

**Step 1: Write failing test**

Create `tests/Mkat.Api.Tests/Workers/HealthCheckWorkerTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Entities;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Workers;
using Monitor = Mkat.Domain.Entities.Monitor;

namespace Mkat.Api.Tests.Workers;

public class HealthCheckWorkerTests
{
    [Fact]
    public async Task CheckHealthChecksAsync_HealthyEndpoint_TransitionsToUp()
    {
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.HealthCheck,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200",
            TimeoutSeconds = 10,
            IntervalSeconds = 60,
            Service = new Service { Id = Guid.NewGuid(), Name = "Test", State = ServiceState.Unknown }
        };

        var monitorRepo = new Mock<IMonitorRepository>();
        monitorRepo.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        var serviceRepo = new Mock<IServiceRepository>();
        serviceRepo.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitor.Service);

        var stateService = new Mock<IStateService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<HealthCheckWorker>>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("healthy")
            });
        var httpClient = new HttpClient(handler.Object);
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton(monitorRepo.Object);
        services.AddSingleton(serviceRepo.Object);
        services.AddSingleton(stateService.Object);
        services.AddSingleton(unitOfWork.Object);
        services.AddSingleton(httpFactory.Object);
        var sp = services.BuildServiceProvider();

        var worker = new HealthCheckWorker(sp, logger.Object);
        await worker.CheckHealthChecksAsync(CancellationToken.None);

        stateService.Verify(s => s.TransitionToUpAsync(
            monitor.ServiceId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecksAsync_UnhealthyEndpoint_TransitionsToDown()
    {
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.HealthCheck,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200",
            TimeoutSeconds = 10,
            IntervalSeconds = 60,
            Service = new Service { Id = Guid.NewGuid(), Name = "Test", State = ServiceState.Up }
        };

        var monitorRepo = new Mock<IMonitorRepository>();
        monitorRepo.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        var serviceRepo = new Mock<IServiceRepository>();
        serviceRepo.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitor.Service);

        var stateService = new Mock<IStateService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<HealthCheckWorker>>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler.Object);
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton(monitorRepo.Object);
        services.AddSingleton(serviceRepo.Object);
        services.AddSingleton(stateService.Object);
        services.AddSingleton(unitOfWork.Object);
        services.AddSingleton(httpFactory.Object);
        var sp = services.BuildServiceProvider();

        var worker = new HealthCheckWorker(sp, logger.Object);
        await worker.CheckHealthChecksAsync(CancellationToken.None);

        stateService.Verify(s => s.TransitionToDownAsync(
            monitor.ServiceId, AlertType.FailedHealthCheck,
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecksAsync_BodyRegexMismatch_TransitionsToDown()
    {
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.HealthCheck,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200",
            TimeoutSeconds = 10,
            IntervalSeconds = 60,
            BodyMatchRegex = "\"status\":\\s*\"ok\"",
            Service = new Service { Id = Guid.NewGuid(), Name = "Test", State = ServiceState.Up }
        };

        var monitorRepo = new Mock<IMonitorRepository>();
        monitorRepo.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        var serviceRepo = new Mock<IServiceRepository>();
        serviceRepo.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitor.Service);

        var stateService = new Mock<IStateService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<HealthCheckWorker>>();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\": \"error\"}")
            });
        var httpClient = new HttpClient(handler.Object);
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton(monitorRepo.Object);
        services.AddSingleton(serviceRepo.Object);
        services.AddSingleton(stateService.Object);
        services.AddSingleton(unitOfWork.Object);
        services.AddSingleton(httpFactory.Object);
        var sp = services.BuildServiceProvider();

        var worker = new HealthCheckWorker(sp, logger.Object);
        await worker.CheckHealthChecksAsync(CancellationToken.None);

        stateService.Verify(s => s.TransitionToDownAsync(
            monitor.ServiceId, AlertType.FailedHealthCheck,
            It.Is<string>(msg => msg.Contains("Body")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthChecksAsync_PausedService_Skipped()
    {
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Type = MonitorType.HealthCheck,
            HealthCheckUrl = "https://example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200",
            TimeoutSeconds = 10,
            IntervalSeconds = 60,
            Service = new Service { Id = Guid.NewGuid(), Name = "Test", State = ServiceState.Paused }
        };

        var monitorRepo = new Mock<IMonitorRepository>();
        monitorRepo.Setup(r => r.GetHealthCheckMonitorsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Monitor> { monitor });

        var serviceRepo = new Mock<IServiceRepository>();
        serviceRepo.Setup(r => r.GetByIdAsync(monitor.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitor.Service);

        var stateService = new Mock<IStateService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<HealthCheckWorker>>();

        var httpFactory = new Mock<IHttpClientFactory>();

        var services = new ServiceCollection();
        services.AddSingleton(monitorRepo.Object);
        services.AddSingleton(serviceRepo.Object);
        services.AddSingleton(stateService.Object);
        services.AddSingleton(unitOfWork.Object);
        services.AddSingleton(httpFactory.Object);
        var sp = services.BuildServiceProvider();

        var worker = new HealthCheckWorker(sp, logger.Object);
        await worker.CheckHealthChecksAsync(CancellationToken.None);

        stateService.Verify(s => s.TransitionToDownAsync(
            It.IsAny<Guid>(), It.IsAny<AlertType>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        stateService.Verify(s => s.TransitionToUpAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

**Note:** Tests use `Moq` and `Moq.Protected` for `HttpMessageHandler` mocking. Add `using Moq.Protected;` at top. The `handler.Protected()` pattern is used because `HttpMessageHandler.SendAsync` is protected.

**Step 2: Run tests to verify RED**

Run: `dotnet test tests/Mkat.Api.Tests --filter "HealthCheckWorker" --verbosity quiet`
Expected: FAIL — `HealthCheckWorker` class doesn't exist

**Step 3: Implement HealthCheckWorker**

Create `src/Mkat.Infrastructure/Workers/HealthCheckWorker.cs`:
```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mkat.Application.Interfaces;
using Mkat.Application.Services;
using Mkat.Domain.Enums;

namespace Mkat.Infrastructure.Workers;

public class HealthCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public HealthCheckWorker(
        IServiceProvider serviceProvider,
        ILogger<HealthCheckWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HealthCheckWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHealthChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HealthCheckWorker");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("HealthCheckWorker stopping");
    }

    public async Task CheckHealthChecksAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var stateService = scope.ServiceProvider.GetRequiredService<IStateService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var now = DateTime.UtcNow;
        var monitors = await monitorRepo.GetHealthCheckMonitorsDueAsync(now, ct);

        foreach (var monitor in monitors)
        {
            var service = await serviceRepo.GetByIdAsync(monitor.ServiceId, ct);
            if (service == null || service.State == ServiceState.Paused)
                continue;

            var (success, reason) = await ExecuteHealthCheckAsync(monitor, httpFactory, ct);

            monitor.LastCheckIn = DateTime.UtcNow;
            await monitorRepo.UpdateAsync(monitor, ct);
            await unitOfWork.SaveChangesAsync(ct);

            if (success)
            {
                await stateService.TransitionToUpAsync(monitor.ServiceId, "Health check passed", ct);
            }
            else
            {
                _logger.LogWarning(
                    "Health check failed for service {ServiceId}: {Reason}",
                    monitor.ServiceId, reason);

                await stateService.TransitionToDownAsync(
                    monitor.ServiceId,
                    AlertType.FailedHealthCheck,
                    reason,
                    ct);
            }
        }
    }

    private async Task<(bool Success, string Reason)> ExecuteHealthCheckAsync(
        Domain.Entities.Monitor monitor,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var client = httpFactory.CreateClient("HealthCheck");
        client.Timeout = TimeSpan.FromSeconds(monitor.TimeoutSeconds ?? 10);

        var method = (monitor.HttpMethod?.ToUpperInvariant()) switch
        {
            "HEAD" => HttpMethod.Head,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            _ => HttpMethod.Get
        };

        try
        {
            using var request = new HttpRequestMessage(method, monitor.HealthCheckUrl);
            using var response = await client.SendAsync(request, ct);

            var statusCode = (int)response.StatusCode;
            var expectedCodes = ParseExpectedStatusCodes(monitor.ExpectedStatusCodes ?? "200");

            if (!expectedCodes.Contains(statusCode))
            {
                return (false, $"Unexpected status code: {statusCode}. Expected: {monitor.ExpectedStatusCodes ?? "200"}");
            }

            if (!string.IsNullOrEmpty(monitor.BodyMatchRegex))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!Regex.IsMatch(body, monitor.BodyMatchRegex))
                {
                    return (false, $"Body did not match pattern: {monitor.BodyMatchRegex}");
                }
            }

            return (true, string.Empty);
        }
        catch (TaskCanceledException)
        {
            return (false, $"Health check timed out after {monitor.TimeoutSeconds ?? 10}s");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    private static HashSet<int> ParseExpectedStatusCodes(string codes)
    {
        var result = new HashSet<int>();
        foreach (var code in codes.Split(','))
        {
            if (int.TryParse(code.Trim(), out var parsed))
                result.Add(parsed);
        }
        return result.Count > 0 ? result : new HashSet<int> { 200 };
    }
}
```

**Step 4: Register worker in Program.cs**

In `src/Mkat.Api/Program.cs`, after the `MetricRetentionWorker` registration, add:
```csharp
    builder.Services.AddHostedService<HealthCheckWorker>();
```

**Step 5: Run tests to verify GREEN**

Run: `dotnet test tests/Mkat.Api.Tests --filter "HealthCheckWorker" --verbosity quiet`
Expected: All 4 tests PASS

**Step 6: Run full suite**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 7: Commit**

```bash
git add src/Mkat.Infrastructure/Workers/HealthCheckWorker.cs src/Mkat.Api/Program.cs tests/Mkat.Api.Tests/Workers/HealthCheckWorkerTests.cs
git commit -m "feat: implement HealthCheckWorker background service"
```

---

### Task 7: Integration tests for health check monitor CRUD

**Files:**
- Test: `tests/Mkat.Api.Tests/Controllers/HealthCheckMonitorTests.cs`

**Step 1: Write integration tests**

Create `tests/Mkat.Api.Tests/Controllers/HealthCheckMonitorTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mkat.Application.DTOs;
using Mkat.Domain.Enums;
using Mkat.Infrastructure.Data;

namespace Mkat.Api.Tests.Controllers;

[Collection("BasicAuth")]
public class HealthCheckMonitorTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckMonitorTests()
    {
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "test123");

        var dbName = $"TestDb_{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<MkatDbContext>));
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

    [Fact]
    public async Task CreateService_WithHealthCheckMonitor_Succeeds()
    {
        var request = new CreateServiceRequest
        {
            Name = "HealthCheck Test Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60,
                    HealthCheckUrl = "https://example.com/health",
                    HttpMethod = "GET",
                    ExpectedStatusCodes = "200,201",
                    TimeoutSeconds = 15,
                    BodyMatchRegex = "ok|healthy"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var service = await response.Content.ReadFromJsonAsync<ServiceResponse>();
        Assert.NotNull(service);
        Assert.Single(service!.Monitors);
        var monitor = service.Monitors[0];
        Assert.Equal(MonitorType.HealthCheck, monitor.Type);
        Assert.Equal("https://example.com/health", monitor.HealthCheckUrl);
        Assert.Equal("GET", monitor.HttpMethod);
        Assert.Equal("200,201", monitor.ExpectedStatusCodes);
        Assert.Equal(15, monitor.TimeoutSeconds);
        Assert.Equal("ok|healthy", monitor.BodyMatchRegex);
    }

    [Fact]
    public async Task CreateService_HealthCheckWithoutUrl_Returns400()
    {
        var request = new CreateServiceRequest
        {
            Name = "Bad HealthCheck Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateService_HealthCheckWithDefaults_UsesDefaults()
    {
        var request = new CreateServiceRequest
        {
            Name = "Default HealthCheck Service",
            Severity = Severity.Medium,
            Monitors = new List<CreateMonitorRequest>
            {
                new()
                {
                    Type = MonitorType.HealthCheck,
                    IntervalSeconds = 60,
                    HealthCheckUrl = "https://example.com/health"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/services", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var service = await response.Content.ReadFromJsonAsync<ServiceResponse>();
        var monitor = service!.Monitors[0];
        Assert.Equal("GET", monitor.HttpMethod);
        Assert.Equal("200", monitor.ExpectedStatusCodes);
        Assert.Equal(10, monitor.TimeoutSeconds);
        Assert.Null(monitor.BodyMatchRegex);
    }
}
```

**Step 2: Run tests**

Run: `dotnet test tests/Mkat.Api.Tests --filter "HealthCheckMonitor" --verbosity quiet`
Expected: All 3 PASS

**Step 3: Run full suite**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 4: Commit**

```bash
git add tests/Mkat.Api.Tests/Controllers/HealthCheckMonitorTests.cs
git commit -m "test: add integration tests for health check monitor CRUD"
```

---

### Task 8: Frontend — add health check fields to types and forms

**Files:**
- Modify: `src/mkat-ui/src/api/types.ts`
- Modify: `src/mkat-ui/src/components/services/ServiceForm.tsx`
- Modify: `src/mkat-ui/src/pages/ServiceDetail.tsx`
- Modify: `src/mkat-ui/src/pages/ServiceEdit.tsx`
- Modify: `src/mkat-ui/src/components/monitors/MonitorDescription.tsx`

**Step 1: Update TypeScript types**

In `src/mkat-ui/src/api/types.ts`, add to `Monitor` interface after the metric fields:
```typescript
  // Health check monitor fields
  healthCheckUrl: string | null;
  httpMethod: string | null;
  expectedStatusCodes: string | null;
  timeoutSeconds: number | null;
  bodyMatchRegex: string | null;
```

Add to `CreateMonitorRequest` and `UpdateMonitorRequest`:
```typescript
  healthCheckUrl?: string;
  httpMethod?: string;
  expectedStatusCodes?: string;
  timeoutSeconds?: number;
  bodyMatchRegex?: string;
```

**Step 2: Update ServiceForm**

In `src/mkat-ui/src/components/services/ServiceForm.tsx`, after the Metric conditional fields block, add:
```tsx
                {monitor.type === MonitorType.HealthCheck && (
                  <div className="space-y-3 border-t pt-3 mt-3">
                    <div>
                      <label className="block text-xs text-gray-600">URL</label>
                      <input
                        type="url"
                        value={monitor.healthCheckUrl ?? ''}
                        onChange={e => updateMonitor(index, 'healthCheckUrl', e.target.value || undefined)}
                        className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                        placeholder="https://example.com/health"
                        required
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="block text-xs text-gray-600">HTTP Method</label>
                        <select
                          value={monitor.httpMethod ?? 'GET'}
                          onChange={e => updateMonitor(index, 'httpMethod', e.target.value)}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                        >
                          <option value="GET">GET</option>
                          <option value="HEAD">HEAD</option>
                          <option value="POST">POST</option>
                          <option value="PUT">PUT</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600">Timeout (seconds)</label>
                        <input
                          type="number"
                          value={monitor.timeoutSeconds ?? 10}
                          onChange={e => updateMonitor(index, 'timeoutSeconds', Number(e.target.value))}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                          min={1}
                          max={120}
                        />
                      </div>
                    </div>
                    <div>
                      <label className="block text-xs text-gray-600">Expected Status Codes</label>
                      <input
                        type="text"
                        value={monitor.expectedStatusCodes ?? '200'}
                        onChange={e => updateMonitor(index, 'expectedStatusCodes', e.target.value)}
                        className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                        placeholder="200,201,204"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-600">Body Match Regex (optional)</label>
                      <input
                        type="text"
                        value={monitor.bodyMatchRegex ?? ''}
                        onChange={e => updateMonitor(index, 'bodyMatchRegex', e.target.value || undefined)}
                        className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                        placeholder="ok|healthy"
                      />
                    </div>
                  </div>
                )}
```

Also update the `updateMonitor` function signature to accept `string | number | undefined` (currently only accepts `number | undefined`). And reset health check fields when switching away from HealthCheck type, similar to the Metric reset pattern.

**Step 3: Update ServiceDetail**

In `src/mkat-ui/src/pages/ServiceDetail.tsx`, after the Heartbeat section, add:
```tsx
              {monitor.type === MonitorType.HealthCheck && (
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm text-gray-600">
                    <span>URL: {monitor.healthCheckUrl}</span>
                    <span>Method: {monitor.httpMethod ?? 'GET'}</span>
                    <span>Expected: {monitor.expectedStatusCodes ?? '200'}</span>
                    <span>Timeout: {monitor.timeoutSeconds ?? 10}s</span>
                    {monitor.bodyMatchRegex && (
                      <span className="col-span-2">Body match: <code className="bg-gray-100 px-1 rounded">{monitor.bodyMatchRegex}</code></span>
                    )}
                  </div>
                </div>
              )}
```

**Step 4: Update ServiceEdit**

In `src/mkat-ui/src/pages/ServiceEdit.tsx`, add the same health check form fields as ServiceForm if monitors are editable there. Check the existing pattern — if it delegates to `ServiceForm`, the changes may already be inherited.

**Step 5: Update MonitorDescription**

In `src/mkat-ui/src/components/monitors/MonitorDescription.tsx`, update the HealthCheck entry:
```typescript
  [MonitorType.HealthCheck]: {
    label: 'Health Check',
    summary: 'mkat actively polls an HTTP endpoint on a schedule and alerts on failures.',
    detail:
      'Configure a URL, HTTP method, expected status codes, and optional response body regex. mkat sends the request at each interval. If the status code is unexpected, the body doesn\'t match, the request times out, or the connection fails, the service is marked as down. Recovery is automatic when the next check succeeds.',
  },
```

**Step 6: TypeScript check**

Run: `cd src/mkat-ui && npx tsc --noEmit`
Expected: No errors

**Step 7: Commit**

```bash
git add src/mkat-ui/
git commit -m "feat: add health check configuration to frontend forms and detail page"
```

---

### Task 9: Build frontend and run full verification

**Step 1: Build frontend**

Run: `cd src/mkat-ui && npx vite build`
Expected: Build succeeds

**Step 2: Run full .NET test suite**

Run: `dotnet test --verbosity quiet`
Expected: All PASS

**Step 3: Commit built assets**

```bash
git add src/Mkat.Api/wwwroot/
git commit -m "chore: rebuild frontend with health check monitor support"
```

---
