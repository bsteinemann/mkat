# Plan: User Documentation Site (VitePress)

## Goal
Add a VitePress documentation site served from the Docker container at `/docs`, covering Getting Started, Concepts, and Use Case Recipes.

---

## Task 1: Scaffold VitePress project

**Files:** `docs-site/package.json`, `docs-site/.vitepress/config.ts`

- Create `docs-site/` at repo root with VitePress dependency
- Configure `base: '/docs/'`, title, nav, sidebar
- Run `npm install` to generate lock file

## Task 2: Write content pages

**Files:**
- `docs-site/index.md` - Landing page
- `docs-site/getting-started.md` - Install, create service, receive first alert
- `docs-site/concepts/services.md` - Services, severity, state machine
- `docs-site/concepts/monitors.md` - Webhook vs Heartbeat, interval, grace period
- `docs-site/concepts/alerts.md` - Alert lifecycle, acknowledge, mute
- `docs-site/recipes/cron-job.md` - Monitor a cron/scheduled task with heartbeat
- `docs-site/recipes/web-api.md` - Monitor a web API with webhook
- `docs-site/recipes/telegram.md` - Adapted from docs/telegram-setup.md
- `docs-site/api-reference.md` - Adapted from docs/api.md

## Task 3: Add URL rewrite middleware for clean URLs

**File:** `src/Mkat.Api/Program.cs`

Add before `UseDefaultFiles()`:
- `/docs` → redirect to `/docs/`
- `/docs/getting-started` → rewrite to `/docs/getting-started.html`

This prevents the SPA fallback from catching docs paths.

```csharp
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    if (path == "/docs")
    {
        context.Response.Redirect("/docs/", permanent: false);
        return;
    }

    if (path.StartsWith("/docs/") && !Path.HasExtension(path) && !path.EndsWith("/"))
    {
        var htmlPath = path + ".html";
        var filePath = Path.Combine(app.Environment.WebRootPath, htmlPath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            context.Request.Path = htmlPath;
        }
    }

    await next();
});
```

## Task 4: Update Dockerfile with docs-build stage

**File:** `Dockerfile`

Add a `docs-build` stage (node:20-alpine) that runs `npm ci && npm run build`. Copy `.vitepress/dist` output to `wwwroot/docs/` in the backend-build stage, after the frontend COPY.

```dockerfile
# Stage 1b: Build documentation site
FROM node:20-alpine AS docs-build
WORKDIR /docs
COPY docs-site/package.json docs-site/package-lock.json ./
RUN npm ci
COPY docs-site/ ./
RUN npm run build

# In backend-build stage, after frontend COPY:
COPY --from=docs-build /docs/.vitepress/dist src/Mkat.Api/wwwroot/docs/
```

## Task 5: Update .gitignore and .dockerignore

- `.gitignore`: add `docs-site/.vitepress/dist/`, `docs-site/.vitepress/cache/`, `docs-site/node_modules/`
- `.dockerignore`: add `docs-site/node_modules`, `docs-site/.vitepress/cache`

## Task 6: Verify locally

- Build Docker image, run container
- Verify `/docs/` serves VitePress landing page
- Verify `/docs/getting-started` serves correct page (clean URL)
- Verify SPA still works at `/`
- Verify `/api/v1/services` still works

---

## Key Architecture Decisions

- **Separate from `docs/`**: The existing `docs/` folder is internal dev docs (plans, ADRs, learnings). User docs live in `docs-site/`.
- **Clean URLs via middleware**: VitePress generates `.html` files. A small rewrite middleware maps `/docs/foo` → `/docs/foo.html` before static files middleware runs.
- **No auth on docs**: `BasicAuthMiddleware` only protects `/api/` paths, so docs are publicly accessible.
- **Separate Docker build stage**: Docs and SPA have independent dependency trees, separate stages improve caching.
