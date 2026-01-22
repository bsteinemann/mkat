# Stage 1: Build frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src
COPY src/mkat-ui/package.json src/mkat-ui/package-lock.json ./
RUN npm ci
COPY src/mkat-ui/ ./
RUN npx vite build --outDir /src/dist

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

# Copy solution and project files for restore
COPY Mkat.sln .
COPY src/Mkat.Domain/Mkat.Domain.csproj src/Mkat.Domain/
COPY src/Mkat.Application/Mkat.Application.csproj src/Mkat.Application/
COPY src/Mkat.Infrastructure/Mkat.Infrastructure.csproj src/Mkat.Infrastructure/
COPY src/Mkat.Api/Mkat.Api.csproj src/Mkat.Api/

RUN dotnet restore src/Mkat.Api/Mkat.Api.csproj

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
