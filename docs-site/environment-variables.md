# Environment Variables

All configuration is done through environment variables.

## Required

| Variable | Description |
|----------|-------------|
| `MKAT_USERNAME` | Username for basic authentication |
| `MKAT_PASSWORD` | Password for basic authentication |

## Notifications

| Variable | Description |
|----------|-------------|
| `MKAT_TELEGRAM_BOT_TOKEN` | Telegram bot token (from [@BotFather](https://t.me/botfather)) |
| `MKAT_TELEGRAM_CHAT_ID` | Telegram chat ID for the default notification channel |

These are optional if you configure notification channels per-contact via the API instead.

## Database

| Variable | Default | Description |
|----------|---------|-------------|
| `MKAT_DATABASE_PATH` | `mkat.db` | Path to the SQLite database file |

## Logging

| Variable | Default | Description |
|----------|---------|-------------|
| `MKAT_LOG_LEVEL` | `Information` | Minimum log level: `Debug`, `Information`, `Warning`, `Error` |

## Application

| Variable | Default | Description |
|----------|---------|-------------|
| `MKAT_BASE_PATH` | `/mkat` | URL path prefix for the application |
