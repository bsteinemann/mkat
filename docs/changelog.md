# Changelog

All notable changes to mkat, ordered newest-first.

---

## [Unreleased]

### 2026-01-22 - M2: Core API

- Added BasicAuthMiddleware (skips /health, /webhook, /heartbeat; 500 if password not configured)
- Created DTOs: CreateServiceRequest, UpdateServiceRequest, ServiceResponse, MonitorResponse, PagedResponse, ErrorResponse
- Created FluentValidation validators for create/update requests
- Implemented ServicesController with full CRUD (POST, GET, GET/{id}, PUT/{id}, DELETE/{id})
- Pagination support with configurable page size (max 100)
- Monitor URL generation (webhook fail/recover, heartbeat)
- 38 API integration tests, 29 application tests passing

### 2026-01-22 - M1: Foundation

- Set up Clean Architecture project structure (Domain, Application, Infrastructure, Api)
- Created domain entities (Service, Monitor, Alert, NotificationChannel, MuteWindow)
- Created EF Core DbContext and repositories
- Added initial SQLite migration
- Set up dev container configuration
- 56 domain unit tests passing

### 2026-01-22 - Project Documentation

- Created PRD with full requirements specification
- Created consolidated architecture document
- Created 6-milestone roadmap
- Created implementation plans (M1-M6)
- Set up AI agent workflow system (CLAUDE.md, workflow, learnings, ADRs)
