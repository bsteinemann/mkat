# Implementation Plan: M2 Core API

**Milestone:** 2 - Core API
**Goal:** Service CRUD operations with authentication
**Dependencies:** M1 Foundation

---

## 1. Basic Auth Middleware

### 1.1 Auth Configuration

**File:** `src/Mkat.Api/Middleware/BasicAuthMiddleware.cs`
```csharp
namespace Mkat.Api.Middleware;

public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicAuthMiddleware> _logger;

    public BasicAuthMiddleware(RequestDelegate next, ILogger<BasicAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health endpoints and webhooks
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health") ||
            path.StartsWith("/webhook") ||
            path.StartsWith("/heartbeat"))
        {
            await _next(context);
            return;
        }

        // Check for Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"mkat\"");
            return;
        }

        try
        {
            var authValue = authHeader.ToString();
            if (!authValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var encodedCredentials = authValue["Basic ".Length..].Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var username = parts[0];
            var password = parts[1];

            var expectedUsername = Environment.GetEnvironmentVariable("MKAT_USERNAME") ?? "admin";
            var expectedPassword = Environment.GetEnvironmentVariable("MKAT_PASSWORD");

            if (string.IsNullOrEmpty(expectedPassword))
            {
                _logger.LogWarning("MKAT_PASSWORD environment variable not set");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Server misconfigured" });
                return;
            }

            if (username != expectedUsername || password != expectedPassword)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", username);
                context.Response.StatusCode = 401;
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authentication");
            context.Response.StatusCode = 401;
        }
    }
}
```

### 1.2 Register Middleware

**Update:** `src/Mkat.Api/Program.cs`
```csharp
// Add before app.MapControllers()
app.UseMiddleware<BasicAuthMiddleware>();
```

---

## 2. API Versioning

### 2.1 Configure Routing

**Update:** `src/Mkat.Api/Program.cs`
```csharp
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
```

### 2.2 Controller Base Route

All controllers will use `[Route("api/v1/[controller]")]` attribute.

---

## 3. DTOs

### 3.1 Request DTOs

**File:** `src/Mkat.Application/DTOs/CreateServiceRequest.cs`
```csharp
namespace Mkat.Application.DTOs;

public record CreateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Severity Severity { get; init; } = Severity.Medium;
    public List<CreateMonitorRequest> Monitors { get; init; } = new();
}

public record CreateMonitorRequest
{
    public MonitorType Type { get; init; }
    public int IntervalSeconds { get; init; }
    public int? GracePeriodSeconds { get; init; }
}
```

**File:** `src/Mkat.Application/DTOs/UpdateServiceRequest.cs`
```csharp
namespace Mkat.Application.DTOs;

public record UpdateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Severity Severity { get; init; }
}
```

### 3.2 Response DTOs

**File:** `src/Mkat.Application/DTOs/ServiceResponse.cs`
```csharp
namespace Mkat.Application.DTOs;

public record ServiceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceState State { get; init; }
    public Severity Severity { get; init; }
    public DateTime? PausedUntil { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<MonitorResponse> Monitors { get; init; } = new();
}

public record MonitorResponse
{
    public Guid Id { get; init; }
    public MonitorType Type { get; init; }
    public string Token { get; init; } = string.Empty;
    public int IntervalSeconds { get; init; }
    public int GracePeriodSeconds { get; init; }
    public DateTime? LastCheckIn { get; init; }
    public string WebhookFailUrl { get; init; } = string.Empty;
    public string WebhookRecoverUrl { get; init; } = string.Empty;
    public string HeartbeatUrl { get; init; } = string.Empty;
}
```

**File:** `src/Mkat.Application/DTOs/PagedResponse.cs`
```csharp
namespace Mkat.Application.DTOs;

public record PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

**File:** `src/Mkat.Application/DTOs/ErrorResponse.cs`
```csharp
namespace Mkat.Application.DTOs;

public record ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public Dictionary<string, string[]>? Details { get; init; }
}
```

---

## 4. Validators

### 4.1 NuGet Package

```bash
cd src/Mkat.Application
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### 4.2 Validators

**File:** `src/Mkat.Application/Validators/CreateServiceValidator.cs`
```csharp
namespace Mkat.Application.Validators;

public class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid severity value");

        RuleFor(x => x.Monitors)
            .NotEmpty().WithMessage("At least one monitor is required");

        RuleForEach(x => x.Monitors).SetValidator(new CreateMonitorValidator());
    }
}

public class CreateMonitorValidator : AbstractValidator<CreateMonitorRequest>
{
    public CreateMonitorValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid monitor type")
            .Must(t => t != MonitorType.HealthCheck)
            .WithMessage("Health check monitors are not supported yet");

        RuleFor(x => x.IntervalSeconds)
            .GreaterThanOrEqualTo(30).WithMessage("Interval must be at least 30 seconds")
            .LessThanOrEqualTo(604800).WithMessage("Interval must be 7 days or less");

        RuleFor(x => x.GracePeriodSeconds)
            .GreaterThanOrEqualTo(60).When(x => x.GracePeriodSeconds.HasValue)
            .WithMessage("Grace period must be at least 60 seconds");
    }
}
```

**File:** `src/Mkat.Application/Validators/UpdateServiceValidator.cs`
```csharp
namespace Mkat.Application.Validators;

public class UpdateServiceValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid severity value");
    }
}
```

---

## 5. Service Controller

**File:** `src/Mkat.Api/Controllers/ServicesController.cs`
```csharp
namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepo;
    private readonly IMonitorRepository _monitorRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateServiceRequest> _createValidator;
    private readonly IValidator<UpdateServiceRequest> _updateValidator;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceRepository serviceRepo,
        IMonitorRepository monitorRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateServiceRequest> createValidator,
        IValidator<UpdateServiceRequest> updateValidator,
        ILogger<ServicesController> logger)
    {
        _serviceRepo = serviceRepo;
        _monitorRepo = monitorRepo;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ServiceResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var skip = (page - 1) * pageSize;
        var services = await _serviceRepo.GetAllAsync(skip, pageSize, ct);
        var totalCount = await _serviceRepo.GetCountAsync(ct);

        var response = new PagedResponse<ServiceResponse>
        {
            Items = services.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        return Ok(MapToResponse(service));
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Severity = request.Severity,
            State = ServiceState.Unknown
        };

        foreach (var monitorReq in request.Monitors)
        {
            var gracePeriod = monitorReq.GracePeriodSeconds
                ?? Math.Max(60, monitorReq.IntervalSeconds / 10);

            service.Monitors.Add(new Monitor
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                Type = monitorReq.Type,
                Token = Guid.NewGuid().ToString("N"),
                IntervalSeconds = monitorReq.IntervalSeconds,
                GracePeriodSeconds = gracePeriod
            });
        }

        await _serviceRepo.AddAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created service {ServiceId} with name {ServiceName}",
            service.Id, service.Name);

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, MapToResponse(service));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> Update(
        Guid id,
        [FromBody] UpdateServiceRequest request,
        CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Validation failed",
                Code = "VALIDATION_ERROR",
                Details = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        service.Name = request.Name;
        service.Description = request.Description;
        service.Severity = request.Severity;

        await _serviceRepo.UpdateAsync(service, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Updated service {ServiceId}", service.Id);

        return Ok(MapToResponse(service));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var service = await _serviceRepo.GetByIdAsync(id, ct);
        if (service == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Service not found",
                Code = "SERVICE_NOT_FOUND"
            });
        }

        await _serviceRepo.DeleteAsync(id, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted service {ServiceId}", id);

        return NoContent();
    }

    private ServiceResponse MapToResponse(Service service)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return new ServiceResponse
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            State = service.State,
            Severity = service.Severity,
            PausedUntil = service.PausedUntil,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt,
            Monitors = service.Monitors.Select(m => new MonitorResponse
            {
                Id = m.Id,
                Type = m.Type,
                Token = m.Token,
                IntervalSeconds = m.IntervalSeconds,
                GracePeriodSeconds = m.GracePeriodSeconds,
                LastCheckIn = m.LastCheckIn,
                WebhookFailUrl = $"{baseUrl}/webhook/{m.Token}/fail",
                WebhookRecoverUrl = $"{baseUrl}/webhook/{m.Token}/recover",
                HeartbeatUrl = $"{baseUrl}/heartbeat/{m.Token}"
            }).ToList()
        };
    }
}
```

---

## 6. DI Registration

**Update:** `src/Mkat.Api/Program.cs`
```csharp
// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceValidator>();

// Or register individually
builder.Services.AddScoped<IValidator<CreateServiceRequest>, CreateServiceValidator>();
builder.Services.AddScoped<IValidator<UpdateServiceRequest>, UpdateServiceValidator>();
```

---

## 7. Verification Checklist

- [ ] Auth middleware rejects requests without credentials
- [ ] Auth middleware accepts valid credentials
- [ ] Auth middleware skips /health and /webhook paths
- [ ] `POST /api/v1/services` creates a service
- [ ] `GET /api/v1/services` returns paginated list
- [ ] `GET /api/v1/services/{id}` returns single service
- [ ] `PUT /api/v1/services/{id}` updates service
- [ ] `DELETE /api/v1/services/{id}` removes service
- [ ] Validation errors return proper format
- [ ] 404 returns proper error response
- [ ] Pagination limits enforced (max 100)
- [ ] Service response includes monitor URLs

---

## 8. Test Cases

### 8.1 Auth Tests
```bash
# No auth - should fail
curl -i http://localhost:8080/api/v1/services

# Wrong credentials - should fail
curl -i -u wrong:wrong http://localhost:8080/api/v1/services

# Correct credentials - should succeed
curl -i -u admin:changeme http://localhost:8080/api/v1/services

# Health endpoint - no auth required
curl -i http://localhost:8080/health
```

### 8.2 CRUD Tests
```bash
# Create service
curl -X POST http://localhost:8080/api/v1/services \
  -u admin:changeme \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My API",
    "description": "Production API server",
    "severity": 2,
    "monitors": [
      {"type": 1, "intervalSeconds": 300}
    ]
  }'

# List services
curl http://localhost:8080/api/v1/services -u admin:changeme

# Get single service
curl http://localhost:8080/api/v1/services/{id} -u admin:changeme

# Update service
curl -X PUT http://localhost:8080/api/v1/services/{id} \
  -u admin:changeme \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated API", "severity": 3}'

# Delete service
curl -X DELETE http://localhost:8080/api/v1/services/{id} -u admin:changeme
```

---

## 9. Files to Create/Update

| File | Action | Purpose |
|------|--------|---------|
| `src/Mkat.Api/Middleware/BasicAuthMiddleware.cs` | Create | Authentication |
| `src/Mkat.Application/DTOs/*.cs` | Create | Request/response models |
| `src/Mkat.Application/Validators/*.cs` | Create | Input validation |
| `src/Mkat.Api/Controllers/ServicesController.cs` | Create | API endpoints |
| `src/Mkat.Api/Program.cs` | Update | DI and middleware |

---

**Status:** Ready for implementation
**Estimated complexity:** Medium
