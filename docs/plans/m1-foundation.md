# Implementation Plan: M1 Foundation

**Milestone:** 1 - Foundation
**Goal:** Project structure, database, and basic infrastructure

---

## 1. Solution Structure

### 1.1 Create Solution

```bash
dotnet new sln -n Mkat
```

### 1.2 Create Projects

```bash
# Domain Layer (no dependencies)
dotnet new classlib -n Mkat.Domain -o src/Mkat.Domain

# Application Layer (depends on Domain)
dotnet new classlib -n Mkat.Application -o src/Mkat.Application

# Infrastructure Layer (depends on Application)
dotnet new classlib -n Mkat.Infrastructure -o src/Mkat.Infrastructure

# API Layer (depends on Infrastructure)
dotnet new webapi -n Mkat.Api -o src/Mkat.Api

# Add to solution
dotnet sln add src/Mkat.Domain
dotnet sln add src/Mkat.Application
dotnet sln add src/Mkat.Infrastructure
dotnet sln add src/Mkat.Api
```

### 1.3 Add Project References

```bash
# Application depends on Domain
dotnet add src/Mkat.Application reference src/Mkat.Domain

# Infrastructure depends on Application (and transitively Domain)
dotnet add src/Mkat.Infrastructure reference src/Mkat.Application

# Api depends on Infrastructure (and transitively Application, Domain)
dotnet add src/Mkat.Api reference src/Mkat.Infrastructure
```

### 1.4 Directory Structure

```
mkat/
├── src/
│   ├── Mkat.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── Mkat.Application/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   ├── UseCases/
│   │   ├── DTOs/
│   │   └── Validators/
│   ├── Mkat.Infrastructure/
│   │   ├── Data/
│   │   ├── Repositories/
│   │   ├── Channels/
│   │   └── Workers/
│   └── Mkat.Api/
│       ├── Controllers/
│       ├── Middleware/
│       └── Models/
├── tests/
│   ├── Mkat.Domain.Tests/
│   ├── Mkat.Application.Tests/
│   └── Mkat.Api.Tests/
├── docs/
├── Dockerfile
├── docker-compose.yml
└── Mkat.sln
```

---

## 2. Domain Layer

### 2.1 Enums

**File:** `src/Mkat.Domain/Enums/ServiceState.cs`
```csharp
namespace Mkat.Domain.Enums;

public enum ServiceState
{
    Unknown = 0,
    Up = 1,
    Down = 2,
    Paused = 3
}
```

**File:** `src/Mkat.Domain/Enums/MonitorType.cs`
```csharp
namespace Mkat.Domain.Enums;

public enum MonitorType
{
    Webhook = 0,
    Heartbeat = 1,
    HealthCheck = 2  // Phase 2
}
```

**File:** `src/Mkat.Domain/Enums/AlertType.cs`
```csharp
namespace Mkat.Domain.Enums;

public enum AlertType
{
    Failure = 0,
    Recovery = 1,
    MissedHeartbeat = 2
}
```

**File:** `src/Mkat.Domain/Enums/Severity.cs`
```csharp
namespace Mkat.Domain.Enums;

public enum Severity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

### 2.2 Entities

**File:** `src/Mkat.Domain/Entities/Service.cs`
```csharp
namespace Mkat.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceState State { get; set; } = ServiceState.Unknown;
    public ServiceState? PreviousState { get; set; }  // For resume
    public Severity Severity { get; set; } = Severity.Medium;
    public DateTime? PausedUntil { get; set; }
    public bool AutoResume { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<MuteWindow> MuteWindows { get; set; } = new List<MuteWindow>();
}
```

**File:** `src/Mkat.Domain/Entities/Monitor.cs`
```csharp
namespace Mkat.Domain.Entities;

public class Monitor
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitorType Type { get; set; }
    public string Token { get; set; } = string.Empty;  // UUID for webhook/heartbeat URLs
    public int IntervalSeconds { get; set; }
    public int GracePeriodSeconds { get; set; }
    public string? ConfigJson { get; set; }  // For health check config (Phase 2)
    public DateTime? LastCheckIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
}
```

**File:** `src/Mkat.Domain/Entities/Alert.cs`
```csharp
namespace Mkat.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public AlertType Type { get; set; }
    public Severity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? Metadata { get; set; }  // JSON

    public Service Service { get; set; } = null!;
}
```

**File:** `src/Mkat.Domain/Entities/NotificationChannel.cs`
```csharp
namespace Mkat.Domain.Entities;

public class NotificationChannel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;  // "telegram", "email", etc.
    public string ConfigJson { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**File:** `src/Mkat.Domain/Entities/MuteWindow.cs`
```csharp
namespace Mkat.Domain.Entities;

public class MuteWindow
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Service Service { get; set; } = null!;
}
```

---

## 3. Application Layer

### 3.1 Interfaces

**File:** `src/Mkat.Application/Interfaces/IServiceRepository.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Service>> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<Service> AddAsync(Service service, CancellationToken ct = default);
    Task UpdateAsync(Service service, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**File:** `src/Mkat.Application/Interfaces/IMonitorRepository.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface IMonitorRepository
{
    Task<Monitor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Monitor?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<Monitor>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<IReadOnlyList<Monitor>> GetHeartbeatMonitorsDueAsync(DateTime threshold, CancellationToken ct = default);
    Task<Monitor> AddAsync(Monitor monitor, CancellationToken ct = default);
    Task UpdateAsync(Monitor monitor, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**File:** `src/Mkat.Application/Interfaces/IAlertRepository.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetByServiceIdAsync(Guid serviceId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Alert>> GetPendingDispatchAsync(CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<Alert> AddAsync(Alert alert, CancellationToken ct = default);
    Task UpdateAsync(Alert alert, CancellationToken ct = default);
}
```

**File:** `src/Mkat.Application/Interfaces/IUnitOfWork.cs`
```csharp
namespace Mkat.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

---

## 4. Infrastructure Layer

### 4.1 NuGet Packages

```bash
cd src/Mkat.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
```

### 4.2 DbContext

**File:** `src/Mkat.Infrastructure/Data/MkatDbContext.cs`
```csharp
namespace Mkat.Infrastructure.Data;

public class MkatDbContext : DbContext, IUnitOfWork
{
    public MkatDbContext(DbContextOptions<MkatDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Monitor> Monitors => Set<Monitor>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<MuteWindow> MuteWindows => Set<MuteWindow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.State);
        });

        modelBuilder.Entity<Monitor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Monitors)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Service)
                .WithMany(s => s.Alerts)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<MuteWindow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ServiceId, e.EndsAt });
            entity.HasOne(e => e.Service)
                .WithMany(s => s.MuteWindows)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### 4.3 Repository Implementation (Example)

**File:** `src/Mkat.Infrastructure/Repositories/ServiceRepository.cs`
```csharp
namespace Mkat.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly MkatDbContext _context;

    public ServiceRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Services
            .Include(s => s.Monitors)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        return await _context.Services
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _context.Services.CountAsync(ct);
    }

    public async Task<Service> AddAsync(Service service, CancellationToken ct = default)
    {
        service.CreatedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.Services.AddAsync(service, ct);
        return service;
    }

    public Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _context.Services.Update(service);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _context.Services.FindAsync(new object[] { id }, ct);
        if (service != null)
        {
            _context.Services.Remove(service);
        }
    }
}
```

---

## 5. API Layer

### 5.1 NuGet Packages

```bash
cd src/Mkat.Api
dotnet add package Serilog.AspNetCore
```

### 5.2 Program.cs

**File:** `src/Mkat.Api/Program.cs`
```csharp
using Mkat.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Database
var dbPath = builder.Configuration["MKAT_DATABASE_PATH"] ?? "mkat.db";
builder.Services.AddDbContext<MkatDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repositories
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IMonitorRepository, MonitorRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MkatDbContext>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Ensure database created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MkatDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();
app.MapControllers();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (MkatDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.Run();
```

### 5.3 appsettings.json

**File:** `src/Mkat.Api/appsettings.json`
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  },
  "AllowedHosts": "*"
}
```

---

## 6. Docker

### 6.1 Dockerfile

**File:** `Dockerfile`
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY src/Mkat.Domain/*.csproj src/Mkat.Domain/
COPY src/Mkat.Application/*.csproj src/Mkat.Application/
COPY src/Mkat.Infrastructure/*.csproj src/Mkat.Infrastructure/
COPY src/Mkat.Api/*.csproj src/Mkat.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Mkat.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV MKAT_DATABASE_PATH=/data/mkat.db

EXPOSE 8080
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "Mkat.Api.dll"]
```

### 6.2 docker-compose.dev.yml

**File:** `docker-compose.dev.yml`
```yaml
version: '3.8'

services:
  mkat:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      - MKAT_DATABASE_PATH=/data/mkat.db
      - MKAT_USERNAME=admin
      - MKAT_PASSWORD=changeme

volumes:
  mkat-data:
```

---

## 7. Verification Checklist

- [ ] Solution builds: `dotnet build`
- [ ] Database migrations created: `dotnet ef migrations add Initial -p src/Mkat.Infrastructure -s src/Mkat.Api`
- [ ] API starts: `dotnet run --project src/Mkat.Api`
- [ ] Health endpoint returns 200: `curl http://localhost:5000/health`
- [ ] Ready endpoint returns 200: `curl http://localhost:5000/health/ready`
- [ ] Docker builds: `docker build -t mkat .`
- [ ] Docker runs: `docker-compose -f docker-compose.dev.yml up`
- [ ] Database file created in volume

---

## 8. Files to Create

| File | Purpose |
|------|---------|
| `Mkat.sln` | Solution file |
| `src/Mkat.Domain/Mkat.Domain.csproj` | Domain project |
| `src/Mkat.Domain/Entities/*.cs` | Entity classes |
| `src/Mkat.Domain/Enums/*.cs` | Enum definitions |
| `src/Mkat.Application/Mkat.Application.csproj` | Application project |
| `src/Mkat.Application/Interfaces/*.cs` | Repository interfaces |
| `src/Mkat.Infrastructure/Mkat.Infrastructure.csproj` | Infrastructure project |
| `src/Mkat.Infrastructure/Data/MkatDbContext.cs` | EF Core context |
| `src/Mkat.Infrastructure/Repositories/*.cs` | Repository implementations |
| `src/Mkat.Api/Mkat.Api.csproj` | API project |
| `src/Mkat.Api/Program.cs` | Application entry point |
| `src/Mkat.Api/appsettings.json` | Configuration |
| `Dockerfile` | Container build |
| `docker-compose.dev.yml` | Dev environment |
| `.gitignore` | Git ignore rules |

---

**Status:** Ready for implementation
**Estimated complexity:** Medium
