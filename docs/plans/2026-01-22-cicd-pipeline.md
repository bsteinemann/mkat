# CI/CD Pipeline & Production Docker Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Set up a GitHub Actions CI/CD pipeline with semantic versioning that builds, tests, and publishes production Docker images to GHCR on every push to main.

**Architecture:** Two GitHub Actions workflows — a CI workflow for build/test validation on all branches and PRs, and a Release workflow on main that uses semantic-release to determine versions, create GitHub releases, and push tagged Docker images to `ghcr.io/bsteinemann/mkat`. A production `docker-compose.yml` is provided for users to run the app.

**Tech Stack:** GitHub Actions, semantic-release (Node.js), Docker multi-stage builds, GitHub Container Registry (GHCR), .NET 8, Node 20, Vite

---

## Prerequisites

Before starting, the repository needs:
- GitHub Actions enabled (default for GitHub repos)
- GHCR access (enabled by default for GitHub repos, uses `GITHUB_TOKEN`)
- Conventional commits on main branch (already followed per CLAUDE.md)

No secrets need to be configured — `GITHUB_TOKEN` is automatically available and has GHCR write permissions.

---

### Task 1: Create .dockerignore

**Files:**
- Create: `.dockerignore`

**Step 1: Create the .dockerignore file**

```
# Version control
.git
.gitignore

# IDE
.vscode
.idea
*.swp
*.swo

# Development
.devcontainer
docker-compose.dev.yml

# Documentation
docs/
*.md
!README.md

# Tests
tests/

# Node modules (frontend installs in Docker)
src/mkat-ui/node_modules
src/mkat-ui/dist

# Build artifacts
**/bin
**/obj
**/publish

# Environment files
.env
.env.*

# CI/CD
.github

# Claude
.claude
```

**Step 2: Commit**

```bash
git add .dockerignore
git commit -m "chore: add .dockerignore to optimize Docker build context"
```

---

### Task 2: Update Production Dockerfile (Multi-Stage with Frontend)

**Files:**
- Modify: `Dockerfile`

The current Dockerfile only builds the .NET backend. The production image needs to also build the React frontend and serve it as static files from wwwroot.

**Step 1: Replace Dockerfile with multi-stage build**

```dockerfile
# Stage 1: Build frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src
COPY src/mkat-ui/package.json src/mkat-ui/package-lock.json ./
RUN npm ci
COPY src/mkat-ui/ ./
RUN npm run build

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

# Copy solution and project files for restore
COPY Mkat.sln .
COPY src/Mkat.Domain/Mkat.Domain.csproj src/Mkat.Domain/
COPY src/Mkat.Application/Mkat.Application.csproj src/Mkat.Application/
COPY src/Mkat.Infrastructure/Mkat.Infrastructure.csproj src/Mkat.Infrastructure/
COPY src/Mkat.Api/Mkat.Api.csproj src/Mkat.Api/
RUN dotnet restore

# Copy source and frontend build output
COPY src/ src/
COPY --from=frontend-build /src/dist src/Mkat.Api/wwwroot/

# Publish
RUN dotnet publish src/Mkat.Api/Mkat.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user and data directory
RUN addgroup -S mkat && adduser -S mkat -G mkat \
    && mkdir -p /data && chown mkat:mkat /data

COPY --from=backend-build --chown=mkat:mkat /app/publish .

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

**Step 2: Verify Docker build works locally**

Run: `docker build -t mkat:test .`
Expected: Successful build with all three stages completing

**Step 3: Commit**

```bash
git add Dockerfile
git commit -m "feat: update Dockerfile with multi-stage frontend build and security hardening"
```

---

### Task 3: Create Production docker-compose.yml

**Files:**
- Create: `docker-compose.yml`

**Step 1: Create the production docker-compose file**

```yaml
services:
  mkat:
    image: ghcr.io/bsteinemann/mkat:latest
    container_name: mkat
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      - MKAT_USERNAME=${MKAT_USERNAME:?MKAT_USERNAME is required}
      - MKAT_PASSWORD=${MKAT_PASSWORD:?MKAT_PASSWORD is required}
      - MKAT_TELEGRAM_BOT_TOKEN=${MKAT_TELEGRAM_BOT_TOKEN:-}
      - MKAT_TELEGRAM_CHAT_ID=${MKAT_TELEGRAM_CHAT_ID:-}
      - MKAT_DATABASE_PATH=/data/mkat.db
      - MKAT_LOG_LEVEL=${MKAT_LOG_LEVEL:-Information}
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

**Step 2: Commit**

```bash
git add docker-compose.yml
git commit -m "feat: add production docker-compose.yml for deployment"
```

---

### Task 4: Create CI Workflow (Build & Test)

**Files:**
- Create: `.github/workflows/ci.yml`

This workflow runs on all pushes and pull requests. It builds the backend, runs all tests, builds the frontend, and verifies the Docker image builds.

**Step 1: Create the workflow directory and file**

```bash
mkdir -p .github/workflows
```

**Step 2: Create CI workflow**

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    name: Build & Test
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal

  frontend:
    name: Frontend Lint & Build
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: src/mkat-ui/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: src/mkat-ui

      - name: Lint
        run: npm run lint
        working-directory: src/mkat-ui

      - name: Build
        run: npm run build
        working-directory: src/mkat-ui

  docker:
    name: Docker Build
    runs-on: ubuntu-latest
    needs: [test, frontend]

    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build Docker image
        uses: docker/build-push-action@v6
        with:
          context: .
          push: false
          tags: mkat:ci-test
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

**Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add CI workflow for build, test, lint, and Docker verification"
```

---

### Task 5: Configure semantic-release

**Files:**
- Create: `.releaserc.json`
- Create: `.github/workflows/release.yml`

semantic-release runs on pushes to main. It analyzes commit messages (conventional commits), determines the next version, generates a changelog, creates a GitHub release, and triggers the Docker image build+push.

**Step 1: Create .releaserc.json**

```json
{
  "branches": ["main"],
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
      "@semantic-release/changelog",
      {
        "changelogFile": "CHANGELOG.md"
      }
    ],
    [
      "@semantic-release/git",
      {
        "assets": ["CHANGELOG.md"],
        "message": "chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}"
      }
    ],
    "@semantic-release/github"
  ]
}
```

**Step 2: Create Release workflow**

```yaml
name: Release

on:
  push:
    branches: [main]

permissions:
  contents: write
  packages: write
  issues: write
  pull-requests: write

jobs:
  test:
    name: Build & Test
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal

  frontend:
    name: Frontend Lint & Build
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: src/mkat-ui/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: src/mkat-ui

      - name: Lint
        run: npm run lint
        working-directory: src/mkat-ui

      - name: Build
        run: npm run build
        working-directory: src/mkat-ui

  release:
    name: Semantic Release
    runs-on: ubuntu-latest
    needs: [test, frontend]
    outputs:
      new_release_published: ${{ steps.semantic.outputs.new_release_published }}
      new_release_version: ${{ steps.semantic.outputs.new_release_version }}

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
          persist-credentials: false

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install semantic-release
        run: npm install -g semantic-release @semantic-release/changelog @semantic-release/git

      - name: Run semantic-release
        id: semantic
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: npx semantic-release

  docker:
    name: Build & Push Docker Image
    runs-on: ubuntu-latest
    needs: [release]
    if: needs.release.outputs.new_release_published == 'true'

    steps:
      - uses: actions/checkout@v4
        with:
          ref: main

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Docker meta
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ghcr.io/bsteinemann/mkat
          tags: |
            type=semver,pattern={{version}},value=v${{ needs.release.outputs.new_release_version }}
            type=semver,pattern={{major}}.{{minor}},value=v${{ needs.release.outputs.new_release_version }}
            type=semver,pattern={{major}},value=v${{ needs.release.outputs.new_release_version }}
            type=raw,value=latest

      - name: Build and push
        uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          platforms: linux/amd64,linux/arm64
```

**Step 3: Commit**

```bash
git add .releaserc.json .github/workflows/release.yml
git commit -m "ci: add semantic-release config and release workflow with GHCR publishing"
```

---

### Task 6: Ensure package-lock.json exists for frontend CI

**Files:**
- Verify/Create: `src/mkat-ui/package-lock.json`

The CI workflow uses `npm ci` which requires a lockfile. If it doesn't exist, generate it.

**Step 1: Check if package-lock.json exists**

```bash
ls src/mkat-ui/package-lock.json
```

**Step 2: If missing, generate it**

```bash
cd src/mkat-ui && npm install --package-lock-only
```

**Step 3: Commit if generated**

```bash
git add src/mkat-ui/package-lock.json
git commit -m "chore: add package-lock.json for reproducible frontend builds"
```

---

### Task 7: Remove duplicate CI trigger from release.yml

The CI workflow already handles PR validation. The release workflow only needs to run on main pushes. However, both currently trigger on `push: main`. To avoid running tests twice on main pushes, update CI to only run tests on PRs.

**Files:**
- Modify: `.github/workflows/ci.yml`

**Step 1: Update CI trigger to only run on PRs (main push is covered by release.yml)**

Change the `on` section of `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  pull_request:
    branches: [main]
```

This ensures:
- PRs to main → CI workflow runs (build, test, lint, docker build)
- Push to main → Release workflow runs (test, semantic-release, docker push)

**Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: scope CI workflow to pull requests only (releases handle main pushes)"
```

---

### Task 8: Add local Docker build and run scripts

**Files:**
- Create: `scripts/docker-build.sh`
- Create: `scripts/docker-run.sh`
- Create: `scripts/docker-stop.sh`

Convenience scripts for building and testing the Docker image locally without needing to remember flags.

**Step 1: Create the scripts directory**

```bash
mkdir -p scripts
```

**Step 2: Create docker-build.sh**

```bash
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

IMAGE_NAME="mkat"
TAG="${1:-local}"

echo "Building ${IMAGE_NAME}:${TAG}..."
docker build -t "${IMAGE_NAME}:${TAG}" "$PROJECT_ROOT"
echo "Done. Image: ${IMAGE_NAME}:${TAG}"
```

**Step 3: Create docker-run.sh**

```bash
#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="mkat"
TAG="${1:-local}"
CONTAINER_NAME="mkat-local"

# Stop existing container if running
if docker ps -q -f name="$CONTAINER_NAME" | grep -q .; then
  echo "Stopping existing container..."
  docker stop "$CONTAINER_NAME" && docker rm "$CONTAINER_NAME"
fi

echo "Starting ${IMAGE_NAME}:${TAG}..."
docker run -d \
  --name "$CONTAINER_NAME" \
  -p 8080:8080 \
  -v mkat-local-data:/data \
  -e MKAT_USERNAME="${MKAT_USERNAME:-admin}" \
  -e MKAT_PASSWORD="${MKAT_PASSWORD:-changeme}" \
  -e MKAT_TELEGRAM_BOT_TOKEN="${MKAT_TELEGRAM_BOT_TOKEN:-}" \
  -e MKAT_TELEGRAM_CHAT_ID="${MKAT_TELEGRAM_CHAT_ID:-}" \
  -e MKAT_LOG_LEVEL="${MKAT_LOG_LEVEL:-Information}" \
  "${IMAGE_NAME}:${TAG}"

echo "Container started: http://localhost:8080"
echo "Logs: docker logs -f $CONTAINER_NAME"
```

**Step 4: Create docker-stop.sh**

```bash
#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME="mkat-local"

if docker ps -q -f name="$CONTAINER_NAME" | grep -q .; then
  echo "Stopping $CONTAINER_NAME..."
  docker stop "$CONTAINER_NAME" && docker rm "$CONTAINER_NAME"
  echo "Stopped."
else
  echo "No running container named $CONTAINER_NAME."
fi
```

**Step 5: Make scripts executable**

```bash
chmod +x scripts/docker-build.sh scripts/docker-run.sh scripts/docker-stop.sh
```

**Step 6: Commit**

```bash
git add scripts/
git commit -m "chore: add local Docker build/run/stop convenience scripts"
```

---

### Task 9: Verify the full pipeline locally

**Step 1: Build image using the local script**

```bash
./scripts/docker-build.sh
```

Expected: All 3 stages (frontend-build, backend-build, runtime) complete successfully. Output: `Done. Image: mkat:local`

**Step 2: Run container using the local script and verify health**

```bash
./scripts/docker-run.sh

# Wait for startup
sleep 5

# Check health
curl -s http://localhost:8080/health
```

Expected: `{"status":"healthy","timestamp":"..."}`

**Step 3: Stop and clean up**

```bash
./scripts/docker-stop.sh
```

**Step 4: Verify docker-compose.yml works**

```bash
MKAT_USERNAME=admin MKAT_PASSWORD=testpass123 docker compose up -d
sleep 5
curl -s http://localhost:8080/health
docker compose down
```

Expected: Same healthy response.

---

## Summary of Files Created/Modified

| File | Action | Purpose |
|------|--------|---------|
| `.dockerignore` | Create | Optimize Docker build context |
| `Dockerfile` | Modify | Multi-stage with frontend, alpine, non-root, healthcheck |
| `docker-compose.yml` | Create | Production deployment compose file |
| `.github/workflows/ci.yml` | Create | PR validation (build, test, lint, docker) |
| `.github/workflows/release.yml` | Create | Semantic release + GHCR image publishing |
| `.releaserc.json` | Create | semantic-release configuration |
| `src/mkat-ui/package-lock.json` | Verify/Create | Required for `npm ci` in CI |
| `scripts/docker-build.sh` | Create | Local image build convenience script |
| `scripts/docker-run.sh` | Create | Local container run convenience script |
| `scripts/docker-stop.sh` | Create | Local container stop convenience script |

## How It Works End-to-End

1. Developer pushes a PR → CI workflow runs build + test + lint + docker build
2. PR is merged to main → Release workflow triggers
3. semantic-release analyzes commits since last release:
   - `feat:` → minor version bump (0.1.0 → 0.2.0)
   - `fix:` → patch bump (0.1.0 → 0.1.1)
   - `feat!:` or `BREAKING CHANGE:` → major bump (0.1.0 → 1.0.0)
4. If new version → CHANGELOG.md updated, GitHub release created, tagged commit pushed
5. Docker job builds multi-platform image (amd64 + arm64) and pushes to GHCR with version tags
6. Users pull `ghcr.io/bsteinemann/mkat:latest` or a specific version tag

## Docker Image Tags Published

For version `1.2.3`:
- `ghcr.io/bsteinemann/mkat:1.2.3` (exact version)
- `ghcr.io/bsteinemann/mkat:1.2` (minor track)
- `ghcr.io/bsteinemann/mkat:1` (major track)
- `ghcr.io/bsteinemann/mkat:latest` (always latest)
