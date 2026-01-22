FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Mkat.sln .
COPY src/Mkat.Domain/Mkat.Domain.csproj src/Mkat.Domain/
COPY src/Mkat.Application/Mkat.Application.csproj src/Mkat.Application/
COPY src/Mkat.Infrastructure/Mkat.Infrastructure.csproj src/Mkat.Infrastructure/
COPY src/Mkat.Api/Mkat.Api.csproj src/Mkat.Api/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY src/ src/

# Build and publish
RUN dotnet publish src/Mkat.Api/Mkat.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite
RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV MKAT_DATABASE_PATH=/data/mkat.db

EXPOSE 8080

VOLUME /data

ENTRYPOINT ["dotnet", "Mkat.Api.dll"]
