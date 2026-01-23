# Configurable Base Path Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow mkat to run under a configurable URL prefix (e.g., `example.com/mkat/`) so it can share a domain with other apps behind a reverse proxy, without requiring a rebuild.

**Architecture:** ASP.NET Core's `UsePathBase()` middleware strips the prefix on incoming requests. The backend injects the configured base path into the served `index.html` at runtime via a small middleware. The frontend reads this config from `window.__MKAT_BASE_PATH__` and uses it for TanStack Router's `basepath` and API client prefixing. Vite builds with relative asset paths (`base: './'`) so no build-time path is baked in.

**Tech Stack:** ASP.NET Core PathBase middleware, custom HTML-injection middleware, TanStack Router `basepath`, Vite relative base

---

## How It Works (for context)

```
Reverse Proxy (nginx/caddy)
  └─ /mkat/* → http://mkat-container:8080/mkat/*

ASP.NET receives: GET /mkat/api/v1/services
  UsePathBase("/mkat") strips prefix → Path = /api/v1/services
  Controller handles normally

ASP.NET receives: GET /mkat/
  UsePathBase strips → Path = /
  SPA fallback serves index.html with injected <script>window.__MKAT_BASE_PATH__="/mkat"</script>

Browser loads page at /mkat/
  Assets: ./assets/main.js resolves to /mkat/assets/main.js (relative paths)
  Router: basepath="/mkat" means route "/" renders at "/mkat/"
  API: fetches /mkat/api/v1/services (base path prepended)
```

**Key insight:** `UsePathBase` does NOT reject requests without the prefix. So `GET /health` (container healthcheck) and `GET /mkat/health` (external) both work.

---

## Task 1: Add `getBasePath` utility to frontend

**Files:**
- Create: `src/mkat-ui/src/config.ts`

**Step 1: Create the config utility**

```typescript
// src/mkat-ui/src/config.ts

declare global {
  interface Window {
    __MKAT_BASE_PATH__?: string;
  }
}

export function getBasePath(): string {
  const base = window.__MKAT_BASE_PATH__ ?? '';
  // Ensure no trailing slash (TanStack Router expects "/mkat" not "/mkat/")
  return base.endsWith('/') ? base.slice(0, -1) : base;
}
```

**Step 2: Commit**

```bash
git add src/mkat-ui/src/config.ts
git commit -m "feat: add getBasePath runtime config utility"
```

---

## Task 2: Update Vite config for relative asset paths

**Files:**
- Modify: `src/mkat-ui/vite.config.ts`

**Step 1: Add `base: './'` to Vite config**

In `vite.config.ts`, add `base: './'` to the defineConfig object. This makes Vite output relative asset paths (e.g., `./assets/main.js` instead of `/assets/main.js`), so assets resolve correctly regardless of the URL prefix.

```typescript
export default defineConfig({
  base: './',
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:8080',
      '/webhook': 'http://localhost:8080',
      '/heartbeat': 'http://localhost:8080',
      '/health': 'http://localhost:8080',
    },
  },
  build: {
    outDir: '../Mkat.Api/wwwroot',
    emptyOutDir: true,
  },
});
```

**Step 2: Rebuild frontend to verify asset paths are relative**

```bash
cd src/mkat-ui && npx vite build
```

Check the generated `wwwroot/index.html` — script/link tags should use `./assets/...` not `/assets/...`.

**Step 3: Commit**

```bash
git add src/mkat-ui/vite.config.ts
git commit -m "feat: configure Vite for relative asset paths"
```

---

## Task 3: Write failing integration test for PathBase behavior

**Files:**
- Modify: `tests/Mkat.Api.Tests/` (new test file or existing)
- Create: `tests/Mkat.Api.Tests/BasePathTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Mkat.Api.Tests/BasePathTests.cs
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Mkat.Infrastructure.Data;

namespace Mkat.Api.Tests;

public class BasePathTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BasePathTests()
    {
        Environment.SetEnvironmentVariable("MKAT_BASE_PATH", "/mkat");
        Environment.SetEnvironmentVariable("MKAT_USERNAME", "admin");
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", "testpass");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MkatDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<MkatDbContext>(options =>
                        options.UseInMemoryDatabase($"BasePathTests_{Guid.NewGuid()}"));
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Don't follow redirects so we can inspect responses
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HealthEndpoint_WithBasePath_ReturnsOk()
    {
        var response = await _client.GetAsync("/mkat/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutBasePath_StillWorks()
    {
        // UsePathBase doesn't reject non-prefixed requests
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithBasePath_RequiresAuth()
    {
        var response = await _client.GetAsync("/mkat/api/v1/services");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithBasePath_ReturnsData()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String("admin:testpass"u8.ToArray()));

        var response = await _client.GetAsync("/mkat/api/v1/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SpaFallback_WithBasePath_ReturnsHtmlWithConfig()
    {
        var response = await _client.GetAsync("/mkat/dashboard");
        var content = await response.Content.ReadAsStringAsync();

        // Should serve index.html with injected config
        Assert.Contains("__MKAT_BASE_PATH__", content);
        Assert.Contains("/mkat", content);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("MKAT_BASE_PATH", null);
        Environment.SetEnvironmentVariable("MKAT_USERNAME", null);
        Environment.SetEnvironmentVariable("MKAT_PASSWORD", null);
        _client.Dispose();
        _factory.Dispose();
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/Mkat.Api.Tests --filter "FullyQualifiedName~BasePathTests" -v n
```

Expected: FAIL (UsePathBase not yet configured, tests for `/mkat/health` will get 404)

**Step 3: Commit**

```bash
git add tests/Mkat.Api.Tests/BasePathTests.cs
git commit -m "test: add failing integration tests for base path support"
```

---

## Task 4: Add UsePathBase and HTML injection to Program.cs

**Files:**
- Modify: `src/Mkat.Api/Program.cs`

**Step 1: Add UsePathBase and replace MapFallbackToFile**

Add `UsePathBase` early in the pipeline (before all other middleware), and replace `MapFallbackToFile("index.html")` with a custom fallback that injects the base path config.

In Program.cs, after `var app = builder.Build();` and after the migration block, add:

```csharp
var basePath = Environment.GetEnvironmentVariable("MKAT_BASE_PATH") ?? "";
if (!string.IsNullOrEmpty(basePath))
{
    // Ensure it starts with / and doesn't end with /
    if (!basePath.StartsWith('/')) basePath = "/" + basePath;
    basePath = basePath.TrimEnd('/');
    app.UsePathBase(basePath);
}
```

This must go BEFORE `app.UseMiddleware<ExceptionHandlingMiddleware>()`.

Then replace the last line `app.MapFallbackToFile("index.html");` with:

```csharp
app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = 404;
        return;
    }

    var html = await File.ReadAllTextAsync(indexPath);

    // Inject runtime config before </head>
    var configScript = $"<script>window.__MKAT_BASE_PATH__=\"{basePath}\";</script>";
    html = html.Replace("</head>", $"{configScript}</head>");

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});
```

**Step 2: Run the tests**

```bash
dotnet test tests/Mkat.Api.Tests --filter "FullyQualifiedName~BasePathTests" -v n
```

Expected: All tests PASS (or some may need minor adjustments based on actual behavior)

**Step 3: Run full test suite to check for regressions**

```bash
dotnet test
```

Expected: ALL GREEN

**Step 4: Commit**

```bash
git add src/Mkat.Api/Program.cs
git commit -m "feat: add UsePathBase and runtime config injection for base path support"
```

---

## Task 5: Update frontend API client to use base path

**Files:**
- Modify: `src/mkat-ui/src/api/client.ts`

**Step 1: Update API_BASE and login redirect**

```typescript
import { getBasePath } from '../config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

export class ApiError extends Error {
  constructor(public status: number, public code: string, message: string) {
    super(message);
  }
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) throw new ApiError(401, 'UNAUTHORIZED', 'Not authenticated');
  return `Basic ${credentials}`;
}

export async function apiRequest<T>(
  method: string,
  path: string,
  body?: unknown
): Promise<T> {
  const response = await fetch(`${getApiBase()}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': getAuthHeader(),
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401) {
    localStorage.removeItem('mkat_credentials');
    window.location.href = `${getBasePath()}/login`;
    throw new ApiError(401, 'UNAUTHORIZED', 'Session expired');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new ApiError(response.status, error.code || 'ERROR', error.error);
  }

  if (response.status === 204) return {} as T;
  return response.json();
}

export const api = {
  get: <T>(path: string) => apiRequest<T>('GET', path),
  post: <T>(path: string, body?: unknown) => apiRequest<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => apiRequest<T>('PUT', path, body),
  delete: <T>(path: string) => apiRequest<T>('DELETE', path),
};
```

**Step 2: Commit**

```bash
git add src/mkat-ui/src/api/client.ts
git commit -m "feat: update API client to use runtime base path"
```

---

## Task 6: Update TanStack Router to use basepath

**Files:**
- Modify: `src/mkat-ui/src/router.tsx`

**Step 1: Add basepath to createRouter**

Add the import and pass `basepath` to `createRouter`:

```typescript
import { getBasePath } from './config';

// ... (all route definitions stay the same) ...

export const router = createRouter({
  routeTree,
  basepath: getBasePath(),
});
```

The route definitions (`path: '/login'`, `path: '/services'`, etc.) stay unchanged — TanStack Router automatically prefixes them with the basepath.

**Step 2: Commit**

```bash
git add src/mkat-ui/src/router.tsx
git commit -m "feat: configure TanStack Router with runtime basepath"
```

---

## Task 7: Update Login.tsx fetch call

**Files:**
- Modify: `src/mkat-ui/src/pages/Login.tsx`

**Step 1: Update the credential validation fetch**

```typescript
import { getBasePath } from '../config';

// In handleSubmit, change:
const response = await fetch('/api/v1/services?page=1&pageSize=1', {
// To:
const response = await fetch(`${getBasePath()}/api/v1/services?page=1&pageSize=1`, {
```

The `navigate({ to: '/' })` call does NOT need changing — TanStack Router handles the basepath prefix automatically for router navigation.

**Step 2: Commit**

```bash
git add src/mkat-ui/src/pages/Login.tsx
git commit -m "feat: update login page to use base path for auth check"
```

---

## Task 8: Update Docker and environment configuration

**Files:**
- Modify: `docker-compose.yml`
- Modify: `docker-compose.dev.yml`
- Modify: `Dockerfile` (healthcheck only)

**Step 1: Add MKAT_BASE_PATH to docker-compose files**

In both compose files, add to the environment section (commented out as optional):

```yaml
# docker-compose.yml
environment:
  # ... existing vars ...
  # - MKAT_BASE_PATH=/mkat  # Optional: set to run under a URL prefix
```

**Step 2: Dockerfile healthcheck stays unchanged**

The healthcheck hits `http://localhost:8080/health` directly, which still works because UsePathBase only strips the prefix when present — it doesn't reject non-prefixed requests.

**Step 3: Commit**

```bash
git add docker-compose.yml docker-compose.dev.yml
git commit -m "docs: add MKAT_BASE_PATH env var to docker-compose files"
```

---

## Task 9: Update documentation

**Files:**
- Modify: `CLAUDE.md` (add env var to table)
- Modify: `docs/architecture.md` (if it has an env vars section)

**Step 1: Add MKAT_BASE_PATH to env var table in CLAUDE.md**

Add to the Environment Variables table:

```markdown
| MKAT_BASE_PATH | No | URL prefix for reverse proxy (default: none, e.g., /mkat) |
```

**Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: document MKAT_BASE_PATH environment variable"
```

---

## Task 10: Run full verification

**Step 1: Build frontend**

```bash
cd src/mkat-ui && npx vite build
```

**Step 2: Run all tests**

```bash
dotnet test
```

Expected: ALL GREEN

**Step 3: Manual smoke test (optional)**

```bash
MKAT_BASE_PATH=/mkat MKAT_USERNAME=admin MKAT_PASSWORD=test dotnet run --project src/Mkat.Api
# Visit http://localhost:8080/mkat/ in browser
```

---

## Task 11: Add learnings entry

**Files:**
- Modify: `docs/learnings.md`

**Step 1: Add retrospective entry**

Add a new entry to learnings.md covering what went well, what tripped up, and any patterns discovered during this implementation.

**Step 2: Commit**

```bash
git add docs/learnings.md
git commit -m "docs: add base path implementation learnings"
```

---

## Notes

- **Dev mode unchanged:** When running `npm run dev` (Vite dev server), `window.__MKAT_BASE_PATH__` is undefined, so `getBasePath()` returns `''` and everything works at root as before.
- **No base path = no change:** When `MKAT_BASE_PATH` is not set, `basePath` is `""`, `UsePathBase` is skipped, and the injected config is `window.__MKAT_BASE_PATH__=""` which getBasePath normalizes to `''`.
- **Webhook/heartbeat URLs:** External services hitting `/webhook/{token}` and `/heartbeat/{token}` need to use the base path prefix (e.g., `/mkat/webhook/{token}`). Document this for users.
- **Peer pairing:** Same applies to peer-to-peer endpoints — peers need the full prefixed URL.
