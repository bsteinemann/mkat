# mkat

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![CI](https://github.com/bsteinemann/mkat/actions/workflows/ci.yml/badge.svg)](https://github.com/bsteinemann/mkat/actions/workflows/ci.yml)
[![Docker](https://img.shields.io/badge/ghcr.io-bsteinemann%2Fmkat-blue)](https://ghcr.io/bsteinemann/mkat)

```
      /\_/\
     ( o.o )   mkat
      > ^ <    always watching
```

**mkat** is a self-hosted monitoring service for homelabs and small web projects.

## Features

- **Webhook Monitoring** - Receive failure/recovery signals from your services
- **Heartbeat Monitoring** - Detect missed check-ins from cron jobs and scheduled tasks
- **Telegram Notifications** - Interactive alerts with acknowledge and mute buttons
- **Web Dashboard** - Monitor all services at a glance
- **Simple Setup** - Single Docker container, minimal configuration

## Quick Start

### Docker Compose

```yaml
services:
  mkat:
    image: ghcr.io/bsteinemann/mkat:latest
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      - MKAT_USERNAME=admin
      - MKAT_PASSWORD=your-secure-password
      - MKAT_TELEGRAM_BOT_TOKEN=your-bot-token
      - MKAT_TELEGRAM_CHAT_ID=your-chat-id

volumes:
  mkat-data:
```

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `MKAT_USERNAME` | Yes | - | Admin username |
| `MKAT_PASSWORD` | Yes | - | Admin password |
| `MKAT_TELEGRAM_BOT_TOKEN` | No | - | Telegram bot token |
| `MKAT_TELEGRAM_CHAT_ID` | No | - | Telegram chat ID |
| `MKAT_DATABASE_PATH` | No | `/data/mkat.db` | SQLite database path |
| `MKAT_LOG_LEVEL` | No | `Information` | Log level |

## Telegram Setup

1. Create a bot via [@BotFather](https://t.me/botfather)
2. Copy the bot token
3. Start a chat with your bot or add it to a group
4. Get your chat ID (use @userinfobot or the API)
5. Set the environment variables

See [docs/telegram-setup.md](docs/telegram-setup.md) for detailed instructions.

## API Reference

See [docs/api.md](docs/api.md) for full API documentation.

### Quick Examples

```bash
# Create a service with heartbeat monitor
curl -X POST http://localhost:8080/api/v1/services \
  -u admin:password \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Backup Job",
    "severity": 2,
    "monitors": [{"type": 1, "intervalSeconds": 3600}]
  }'

# Send heartbeat
curl -X POST http://localhost:8080/heartbeat/{token}

# Report failure
curl -X POST http://localhost:8080/webhook/{token}/fail

# Report recovery
curl -X POST http://localhost:8080/webhook/{token}/recover
```

## Deployment

See [docs/deployment.md](docs/deployment.md) for reverse proxy setup, backups, and upgrade procedures.

## License

MIT
