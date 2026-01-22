FROM mcr.microsoft.com/devcontainers/base:ubuntu

# SQLite tools for inspecting the database
RUN apt-get update && apt-get install -y \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*
