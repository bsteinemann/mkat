# Plan: User Documentation Site (VitePress)

## Goal
Add a VitePress documentation site published to GitHub Pages, covering Getting Started, Concepts, and Use Case Recipes.

---

## Task 1: Scaffold VitePress project

**Files:** `docs-site/package.json`, `docs-site/.vitepress/config.ts`

- Create `docs-site/` at repo root with VitePress dependency
- Configure `base: '/mkat/'`, title, nav, sidebar
- Run `npm install` to generate lock file

## Task 2: Feature audit and screenshot collection

Audit the current app to produce a complete feature inventory before writing docs. This ensures documentation reflects the actual state of the application.

**Output:** `docs-site/feature-inventory.md` (working document, not published)

### Steps:
1. Review API endpoints (`src/Mkat.Api/Controllers/`) and list all user-facing operations
2. Review the frontend (`src/mkat-ui/src/`) and catalog each page/view and its functionality
3. Document the current feature set:
   - Service management (CRUD, state transitions, severity levels)
   - Monitor types and configuration (webhook, heartbeat, intervals, grace periods)
   - Alert system (lifecycle, acknowledge, mute windows)
   - Notification channels (Telegram setup, delivery)
   - Dashboard / overview page
   - Any other implemented features
4. Identify which features need screenshots — request them from the user
   - Place screenshots in `docs-site/public/images/`
   - Name convention: `<page>-<feature>.png` (e.g. `dashboard-overview.png`, `service-edit.png`)
5. Cross-reference against PRD (`docs/telegram_healthcheck_monitoring_prd.md`) to note what's implemented vs planned

### Screenshot requests will include:
- Which page/view to capture
- What state the app should be in (e.g. "a service in DOWN state")
- Desired crop/focus area if relevant

## Task 3: Write content pages

Content must be based on the feature audit from Task 2, not assumptions.

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

## Task 4: Add GitHub Actions workflow for Pages deployment

**File:** `.github/workflows/docs.yml`

Deploy to GitHub Pages on push to `main` when `docs-site/` changes:

```yaml
name: Deploy Docs

on:
  push:
    branches: [main]
    paths: ['docs-site/**']
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: docs-site/package-lock.json
      - run: npm ci
        working-directory: docs-site
      - run: npm run build
        working-directory: docs-site
      - uses: actions/upload-pages-artifact@v3
        with:
          path: docs-site/.vitepress/dist

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    needs: build
    runs-on: ubuntu-latest
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

## Task 5: Update .gitignore

- `.gitignore`: add `docs-site/.vitepress/dist/`, `docs-site/.vitepress/cache/`, `docs-site/node_modules/`

## Task 6: Enable GitHub Pages in repository settings

Manual step: Go to repo Settings → Pages → Source: "GitHub Actions"

## Task 7: Verify deployment

- Push to `main` with docs-site changes
- Verify GitHub Actions workflow runs successfully
- Verify `https://bsteinemann.github.io/mkat/` serves the landing page
- Verify clean URLs work (e.g. `/mkat/getting-started`)

---

## Key Architecture Decisions

- **Separate from `docs/`**: The existing `docs/` folder is internal dev docs (plans, ADRs, learnings). User docs live in `docs-site/`.
- **GitHub Pages deployment**: Docs are published as a static site to GitHub Pages, decoupled from the application Docker image. This keeps the Docker image lean and allows docs updates without redeploying the app.
- **Base path `/mkat/`**: GitHub Pages for project repos serves at `https://<user>.github.io/<repo>/`, so VitePress base must match the repo name.
- **Path-filtered workflow**: The deploy workflow only triggers when files in `docs-site/` change, avoiding unnecessary rebuilds on app-only commits.
