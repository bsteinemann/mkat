```
# mkat

      /\_/\ 
     ( o.o )   mkat
      > ^ <    always watching

**mkat** is a small, watchful monitoring service for homelabs and small web projects.

It focuses on:

**Monitor · Keepalive · Alert · Trigger**

---

## What is mkat?

mkat helps you answer one simple question:

> “Is my stuff still working?”

It provides:
- Health checks for HTTP endpoints
- Heartbeat / keepalive checks (you *must* ping mkat on time)
- Failure & recovery notifications
- Pluggable notification integrations (Telegram first, more later)
- A simple UI to manage checks, services, and alerts

mkat is designed to be:
- self-hosted
- lightweight
- automation-friendly
- pleasant to use from a terminal or browser

---

## Core Concepts

- **Service**  
  Something you care about (API, job, backup, website)

- **Check**  
  A rule that determines if a service is healthy  
  (HTTP check, heartbeat check, webhook-based check)

- **Incident**  
  A service entering a failed state

- **Recovery**  
  A service returning to healthy state

- **Integration**  
  A notification or trigger target (Telegram, Email, Webhook, etc.)

---

## Features (planned)

- HTTP health endpoint monitoring
- Heartbeat / keepalive checks (cron-style expectations)
- Failure & recovery webhooks
- Telegram notifications
- Extensible integration system
- Web UI for configuration & status
- Clean API for automation

---

## Tech Stack

- **Backend / Services:** .NET (Clean Architecture)
- **Validation:** FluentValidation
- **Frontend:** React, TanStack, Tailwind CSS
- **Runtime:** Docker-first, self-hosted

---

## Project Status

🚧 **Early development**

This project is under active design and implementation.  
Expect breaking changes.

---

## Philosophy

mkat is intentionally:
- not an enterprise monitoring platform
- not Kubernetes-first
- not bloated

It’s a tool that quietly watches your systems and taps you on the shoulder when something goes wrong.

---

## License

MIT
```
