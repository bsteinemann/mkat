# Contributing to mkat

Thanks for your interest in contributing to mkat! This document covers the basics for getting started.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/mkat.git`
3. Create a branch: `git checkout -b my-feature`
4. Make your changes
5. Push and open a pull request

## Development Setup

### Prerequisites

- .NET 8+ SDK
- Node.js 18+
- Docker (optional, for container testing)

### Running Locally

```bash
# Backend
dotnet build
dotnet run --project src/Mkat.Api

# Frontend
cd src/mkat-ui
npm install
npm run dev
```

### Running Tests

```bash
dotnet test
cd src/mkat-ui && npm test
```

## Guidelines

- Follow the existing code style
- Write tests for new functionality
- Keep commits small and focused
- Use conventional commit messages: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`
- One concern per pull request

## Architecture

mkat uses a modular monolith with Clean Architecture layers:

```
Domain → Application → Infrastructure → API
```

Dependencies point inward only. See `docs/architecture.md` for details.

## Reporting Issues

- Use GitHub Issues for bug reports and feature requests
- Include steps to reproduce for bugs
- Check existing issues before opening a new one

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
