# Deployment Guide

## Docker Deployment

### Building the Image

```bash
docker build -t mkat:latest .
```

### Running with Docker

```bash
docker run -d \
  --name mkat \
  -p 8080:8080 \
  -v mkat-data:/data \
  -e MKAT_USERNAME=admin \
  -e MKAT_PASSWORD=changeme \
  -e MKAT_TELEGRAM_BOT_TOKEN=your-token \
  -e MKAT_TELEGRAM_CHAT_ID=your-chat-id \
  ghcr.io/bsteinemann/mkat:latest
```

### Docker Compose

See `docker-compose.yml` in the repository root for a complete example.

```bash
MKAT_USERNAME=admin MKAT_PASSWORD=your-password docker compose up -d
```

## Infrastructure Stack (Traefik + Watchtower)

The repository includes `docker-compose.infrastructure.yaml` which provides a production-ready setup with:

- **Traefik** - Reverse proxy with automatic Let's Encrypt TLS certificates
- **Watchtower** - Automatic container image updates

### Environment Variables

Create a `.env` file (or export these variables):

```bash
# Required
DOMAIN=example.com                  # Your domain (mkat runs at mkat.$DOMAIN)
ACME_EMAIL=you@example.com          # Let's Encrypt registration email
MKAT_USERNAME=admin
MKAT_PASSWORD=your-secure-password

# Optional - Traefik dashboard (accessible at traefik.$DOMAIN)
# Generate with: echo $(htpasswd -nB admin) | sed -e 's/\$/\$\$/g'
TRAEFIK_DASHBOARD_AUTH=admin:$$2y$$...

# Optional - Telegram notifications
MKAT_TELEGRAM_BOT_TOKEN=
MKAT_TELEGRAM_CHAT_ID=

# Optional - Watchtower
WATCHTOWER_POLL_INTERVAL=3600       # Check for updates every hour (default)
WATCHTOWER_NOTIFICATIONS=shoutrrr   # Enable notifications via shoutrrr
WATCHTOWER_NOTIFICATION_URL=        # shoutrrr URL for update notifications
```

### DNS Setup

Point these records to your server:

```
mkat.example.com      A    <your-server-ip>
traefik.example.com   A    <your-server-ip>   # optional, for dashboard
```

### Running

```bash
docker compose -f docker-compose.infrastructure.yaml up -d
```

### What It Does

- HTTP traffic on port 80 is automatically redirected to HTTPS on port 443
- TLS certificates are obtained and renewed via Let's Encrypt HTTP challenge
- mkat is exposed at `https://mkat.<DOMAIN>` with no ports published directly
- Watchtower polls for new container images and restarts services automatically
- Traefik dashboard (optional) is available at `https://traefik.<DOMAIN>` behind basic auth

## Database Backup

SQLite database is stored at the configured `MKAT_DATABASE_PATH` (default: `/data/mkat.db`).

### Backup Script

```bash
#!/bin/bash
BACKUP_DIR="/backups/mkat"
DATE=$(date +%Y%m%d_%H%M%S)

docker exec mkat sqlite3 /data/mkat.db ".backup '/data/backup.db'"
docker cp mkat:/data/backup.db "$BACKUP_DIR/mkat_$DATE.db"
docker exec mkat rm /data/backup.db

# Keep only last 7 days
find "$BACKUP_DIR" -name "mkat_*.db" -mtime +7 -delete
```

## Upgrading

If using the infrastructure stack, **Watchtower handles upgrades automatically** by polling for new images and restarting containers.

For manual upgrades:

```bash
docker compose -f docker-compose.infrastructure.yaml pull
docker compose -f docker-compose.infrastructure.yaml up -d
```

## Troubleshooting

### Check logs

```bash
docker logs mkat
docker logs -f mkat  # follow
```

### Database issues

```bash
# Verify database
docker exec mkat sqlite3 /data/mkat.db "PRAGMA integrity_check;"

# Vacuum database
docker exec mkat sqlite3 /data/mkat.db "VACUUM;"
```

### Telegram issues

1. Verify bot token is correct
2. Ensure chat ID is correct (try sending a test message)
3. Check logs for Telegram errors
4. Verify bot has permission to send messages in the chat
