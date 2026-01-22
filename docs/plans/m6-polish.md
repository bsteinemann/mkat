# Implementation Plan: M6 Polish & Documentation

**Milestone:** 6 - Polish & Documentation
**Goal:** Production readiness
**Dependencies:** All previous milestones

---

## 1. Documentation

### 1.1 README Update

**File:** `README.md`
```markdown
# mkat

```
      /\_/\
     ( o.o )   mkat
      > ^ <    always watching
```

**mkat** is a self-hosted monitoring service for homelabs and small web projects.

## Features

- **Webhook Monitoring** - Receive failure/recovery signals from your services
- **Heartbeat Monitoring** - Detect missed check-ins from cron jobs and scheduled tasks
- **Telegram Notifications** - Interactive alerts with acknowledge and mute buttons
- **Web Dashboard** - Monitor all services at a glance
- **Simple Setup** - Single Docker container, minimal configuration

## Quick Start

### Docker Compose

```yaml
version: '3.8'

services:
  mkat:
    image: ghcr.io/yourname/mkat:latest
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      - MKAT_USERNAME=admin
      - MKAT_PASSWORD=your-secure-password
      - MKAT_TELEGRAM_BOT_TOKEN=your-bot-token
      - MKAT_TELEGRAM_CHAT_ID=your-chat-id

volumes:
  mkat-data:
```

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `MKAT_USERNAME` | Yes | - | Admin username |
| `MKAT_PASSWORD` | Yes | - | Admin password |
| `MKAT_TELEGRAM_BOT_TOKEN` | No | - | Telegram bot token |
| `MKAT_TELEGRAM_CHAT_ID` | No | - | Telegram chat ID |
| `MKAT_DATABASE_PATH` | No | `/data/mkat.db` | SQLite database path |
| `MKAT_LOG_LEVEL` | No | `Information` | Log level |

## Telegram Setup

1. Create a bot via [@BotFather](https://t.me/botfather)
2. Copy the bot token
3. Start a chat with your bot or add it to a group
4. Get your chat ID (use @userinfobot or the API)
5. Set the environment variables

## API Reference

See [docs/api.md](docs/api.md) for full API documentation.

### Quick Examples

```bash
# Create a service with heartbeat monitor
curl -X POST http://localhost:8080/api/v1/services \
  -u admin:password \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Backup Job",
    "monitors": [{"type": 1, "intervalSeconds": 3600}]
  }'

# Send heartbeat
curl -X POST http://localhost:8080/heartbeat/{token}

# Report failure
curl -X POST http://localhost:8080/webhook/{token}/fail
```

## License

MIT
```

### 1.2 Deployment Guide

**File:** `docs/deployment.md`
```markdown
# Deployment Guide

## Docker Deployment

### Building the Image

```bash
docker build -t mkat:latest .
```

### Running with Docker

```bash
docker run -d \
  --name mkat \
  -p 8080:8080 \
  -v mkat-data:/data \
  -e MKAT_USERNAME=admin \
  -e MKAT_PASSWORD=changeme \
  -e MKAT_TELEGRAM_BOT_TOKEN=your-token \
  -e MKAT_TELEGRAM_CHAT_ID=your-chat-id \
  mkat:latest
```

### Docker Compose

See `docker-compose.yml` for a complete example.

## Reverse Proxy Setup

### Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name mkat.example.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Traefik

```yaml
http:
  routers:
    mkat:
      rule: "Host(`mkat.example.com`)"
      service: mkat
      tls:
        certResolver: letsencrypt

  services:
    mkat:
      loadBalancer:
        servers:
          - url: "http://mkat:8080"
```

### Caddy

```
mkat.example.com {
    reverse_proxy localhost:8080
}
```

## Database Backup

SQLite database is stored at the configured `MKAT_DATABASE_PATH` (default: `/data/mkat.db`).

### Backup Script

```bash
#!/bin/bash
BACKUP_DIR="/backups/mkat"
DATE=$(date +%Y%m%d_%H%M%S)

docker exec mkat sqlite3 /data/mkat.db ".backup '/data/backup.db'"
docker cp mkat:/data/backup.db "$BACKUP_DIR/mkat_$DATE.db"
docker exec mkat rm /data/backup.db

# Keep only last 7 days
find "$BACKUP_DIR" -name "mkat_*.db" -mtime +7 -delete
```

## Upgrading

1. Pull the new image
2. Stop the container
3. Backup the database
4. Start with new image

```bash
docker pull ghcr.io/yourname/mkat:latest
docker stop mkat
# backup...
docker rm mkat
docker run ... # same as before
```

## Troubleshooting

### Check logs

```bash
docker logs mkat
docker logs -f mkat  # follow
```

### Database issues

```bash
# Verify database
docker exec mkat sqlite3 /data/mkat.db "PRAGMA integrity_check;"

# Vacuum database
docker exec mkat sqlite3 /data/mkat.db "VACUUM;"
```

### Telegram issues

1. Verify bot token is correct
2. Ensure chat ID is correct (try sending a test message)
3. Check logs for Telegram errors
4. Verify bot has permission to send messages in the chat
```

### 1.3 Telegram Setup Guide

**File:** `docs/telegram-setup.md`
```markdown
# Telegram Bot Setup

## Creating the Bot

1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot`
3. Follow the prompts:
   - Name: `mkat` (or your preferred name)
   - Username: `your_mkat_bot` (must be unique)
4. Save the bot token (looks like `123456789:ABCdefGhIJKlmNoPQRsTUVwxyz`)

## Getting Your Chat ID

### Option 1: Personal Chat

1. Send a message to your new bot
2. Visit: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
3. Look for `"chat":{"id":123456789` in the response
4. The number is your chat ID

### Option 2: Group Chat

1. Add the bot to your group
2. Send a message in the group mentioning the bot
3. Visit: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
4. Look for the group chat ID (negative number for groups)

### Option 3: Using @userinfobot

1. Forward a message from your target chat to @userinfobot
2. It will reply with the chat ID

## Configuration

Set these environment variables:

```bash
MKAT_TELEGRAM_BOT_TOKEN=123456789:ABCdefGhIJKlmNoPQRsTUVwxyz
MKAT_TELEGRAM_CHAT_ID=123456789
```

## Available Commands

Once configured, your bot will respond to:

- `/status` - Overview of all services
- `/list` - List all services with states
- `/mute <service> <duration>` - Mute a service (e.g., `/mute MyAPI 1h`)

Duration formats: `15m`, `1h`, `24h`, `7d`

## Inline Buttons

Alert messages include buttons:

- **Acknowledge** - Mark the alert as seen
- **Mute 15m/1h/24h** - Temporarily suppress alerts

## Testing

Send a test notification from the UI settings page, or use the API:

```bash
curl -X POST http://localhost:8080/api/v1/channels/telegram/test \
  -u admin:password
```
```

### 1.4 API Documentation

**File:** `docs/api.md`
```markdown
# API Reference

Base URL: `/api/v1`

All endpoints require Basic Authentication except webhooks and heartbeats.

## Services

### List Services

```
GET /services?page=1&pageSize=20
```

Response:
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1
}
```

### Create Service

```
POST /services
```

Body:
```json
{
  "name": "My API",
  "description": "Production API server",
  "severity": 2,
  "monitors": [
    {
      "type": 1,
      "intervalSeconds": 300,
      "gracePeriodSeconds": 60
    }
  ]
}
```

Monitor types:
- `0` - Webhook
- `1` - Heartbeat

Severity levels:
- `0` - Low
- `1` - Medium
- `2` - High
- `3` - Critical

### Get Service

```
GET /services/{id}
```

### Update Service

```
PUT /services/{id}
```

### Delete Service

```
DELETE /services/{id}
```

### Pause Service

```
POST /services/{id}/pause
```

Body:
```json
{
  "until": "2025-01-01T00:00:00Z",
  "autoResume": true
}
```

### Resume Service

```
POST /services/{id}/resume
```

### Mute Service

```
POST /services/{id}/mute
```

Body:
```json
{
  "durationMinutes": 60,
  "reason": "Planned maintenance"
}
```

## Webhooks (No Auth Required)

### Report Failure

```
POST /webhook/{token}/fail
```

### Report Recovery

```
POST /webhook/{token}/recover
```

## Heartbeat (No Auth Required)

```
POST /heartbeat/{token}
```

## Alerts

### List Alerts

```
GET /alerts?page=1&pageSize=20
```

### Get Alert

```
GET /alerts/{id}
```

### Acknowledge Alert

```
POST /alerts/{id}/ack
```

## Health Checks

### Basic Health

```
GET /health
```

### Readiness

```
GET /health/ready
```

## Error Response Format

```json
{
  "error": "Human readable message",
  "code": "ERROR_CODE",
  "details": {
    "field": ["Validation error message"]
  }
}
```
```

---

## 2. Docker Production Image

### 2.1 Multi-stage Dockerfile

**File:** `Dockerfile`
```dockerfile
# Build frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src
COPY src/mkat-ui/package*.json ./
RUN npm ci
COPY src/mkat-ui/ ./
RUN npm run build

# Build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

COPY *.sln .
COPY src/Mkat.Domain/*.csproj src/Mkat.Domain/
COPY src/Mkat.Application/*.csproj src/Mkat.Application/
COPY src/Mkat.Infrastructure/*.csproj src/Mkat.Infrastructure/
COPY src/Mkat.Api/*.csproj src/Mkat.Api/
RUN dotnet restore

COPY src/ src/
COPY --from=frontend-build /src/dist src/Mkat.Api/wwwroot/
RUN dotnet publish src/Mkat.Api -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S mkat && adduser -S mkat -G mkat

COPY --from=backend-build /app/publish .
RUN chown -R mkat:mkat /app

USER mkat

ENV ASPNETCORE_URLS=http://+:8080
ENV MKAT_DATABASE_PATH=/data/mkat.db
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Mkat.Api.dll"]
```

### 2.2 Production Docker Compose

**File:** `docker-compose.yml`
```yaml
version: '3.8'

services:
  mkat:
    image: ghcr.io/yourname/mkat:latest
    container_name: mkat
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      - MKAT_USERNAME=${MKAT_USERNAME:?required}
      - MKAT_PASSWORD=${MKAT_PASSWORD:?required}
      - MKAT_TELEGRAM_BOT_TOKEN=${MKAT_TELEGRAM_BOT_TOKEN:-}
      - MKAT_TELEGRAM_CHAT_ID=${MKAT_TELEGRAM_CHAT_ID:-}
      - MKAT_DATABASE_PATH=/data/mkat.db
      - MKAT_LOG_LEVEL=Information
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 10s

volumes:
  mkat-data:
    driver: local
```

---

## 3. Logging Improvements

### 3.1 Structured Logging Configuration

**Update:** `src/Mkat.Api/appsettings.json`
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "mkat"
    }
  },
  "AllowedHosts": "*"
}
```

### 3.2 Request Logging

**Update:** `src/Mkat.Api/Program.cs`
```csharp
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
    };
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
```

---

## 4. Error Handling Middleware

**File:** `src/Mkat.Api/Middleware/ExceptionHandlingMiddleware.cs`
```csharp
namespace Mkat.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "An unexpected error occurred",
                code = "INTERNAL_ERROR",
                requestId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
```

---

## 5. Security Checklist

### 5.1 Pre-deployment Checklist

- [ ] `MKAT_PASSWORD` is strong (min 16 chars, mixed case, numbers, symbols)
- [ ] HTTPS enabled via reverse proxy
- [ ] Database file is on a persistent volume
- [ ] Container runs as non-root user
- [ ] Telegram bot token is kept secret
- [ ] No sensitive data in logs
- [ ] CORS configured appropriately
- [ ] Rate limiting considered for public endpoints

### 5.2 Security Headers

**File:** `src/Mkat.Api/Middleware/SecurityHeadersMiddleware.cs`
```csharp
namespace Mkat.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
        }

        await _next(context);
    }
}
```

---

## 6. Final Verification Checklist

### 6.1 Functionality

- [ ] Service CRUD works
- [ ] Webhook fail/recover works
- [ ] Heartbeat monitoring works
- [ ] Missed heartbeat detection works
- [ ] Pause/resume with auto-resume works
- [ ] Telegram notifications sent
- [ ] Telegram buttons work
- [ ] Telegram commands work
- [ ] Alert acknowledgment works
- [ ] Mute functionality works
- [ ] Dashboard shows correct data
- [ ] All UI pages load correctly
- [ ] Login/logout works
- [ ] API returns proper errors

### 6.2 Operations

- [ ] Docker image builds
- [ ] Container starts and stays healthy
- [ ] Health endpoints respond
- [ ] Logs are structured JSON
- [ ] Database persists across restarts
- [ ] Graceful shutdown works

### 6.3 Documentation

- [ ] README is complete
- [ ] API docs are accurate
- [ ] Telegram setup guide works
- [ ] Deployment guide is clear
- [ ] Environment variables documented

---

## 7. Files to Create/Update

| File | Purpose |
|------|---------|
| `README.md` | Update with full documentation |
| `docs/deployment.md` | Deployment guide |
| `docs/telegram-setup.md` | Telegram configuration |
| `docs/api.md` | API reference |
| `Dockerfile` | Production multi-stage build |
| `docker-compose.yml` | Production compose file |
| `.dockerignore` | Exclude unnecessary files |
| `src/Mkat.Api/appsettings.json` | Logging configuration |
| `src/Mkat.Api/Middleware/*.cs` | Security/error middleware |

---

**Status:** Ready for implementation
**Estimated complexity:** Medium
