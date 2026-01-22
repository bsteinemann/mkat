#!/bin/bash
set -e

# Install .NET EF Core tools
dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef

# Install Claude CLI
npm install -g @anthropic-ai/claude-code

# Restore .NET packages (if solution exists)
if [ -f "Mkat.sln" ]; then
  dotnet restore
fi

# Install frontend dependencies (if package.json exists)
if [ -f "src/mkat-ui/package.json" ]; then
  cd src/mkat-ui && npm install
fi

echo "Dev container setup complete."
