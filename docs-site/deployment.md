# Deployment

## Docker

### Basic

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

```yaml
services:
  mkat:
    image: ghcr.io/bsteinemann/mkat:latest
    ports:
      - "8080:8080"
    volumes:
      - mkat-data:/data
    environment:
      MKAT_USERNAME: admin
      MKAT_PASSWORD: ${MKAT_PASSWORD}
      MKAT_TELEGRAM_BOT_TOKEN: ${MKAT_TELEGRAM_BOT_TOKEN}
      MKAT_TELEGRAM_CHAT_ID: ${MKAT_TELEGRAM_CHAT_ID}

volumes:
  mkat-data:
```

```bash
MKAT_PASSWORD=your-password docker compose up -d
```

## Production Setup with Traefik

The repository includes `docker-compose.infrastructure.yaml` for a production-ready setup with automatic TLS via Let's Encrypt and automatic container updates via Watchtower.

### DNS

Point these records to your server:

```
mkat.example.com      A    <your-server-ip>
traefik.example.com   A    <your-server-ip>   # optional, for Traefik dashboard
```

### Environment

Create a `.env` file:

```bash
DOMAIN=example.com
ACME_EMAIL=you@example.com
MKAT_USERNAME=admin
MKAT_PASSWORD=your-secure-password

# Optional
MKAT_TELEGRAM_BOT_TOKEN=
MKAT_TELEGRAM_CHAT_ID=
TRAEFIK_DASHBOARD_AUTH=admin:$$2y$$...  # htpasswd hash
```

### Run

```bash
docker compose -f docker-compose.infrastructure.yaml up -d
```

This gives you:
- HTTPS at `https://mkat.example.com` with auto-renewed certificates
- Automatic container updates via Watchtower
- Traefik dashboard at `https://traefik.example.com` (optional)

## Database Backup

The SQLite database is stored at `/data/mkat.db` inside the container.

```bash
#!/bin/bash
BACKUP_DIR="/backups/mkat"
DATE=$(date +%Y%m%d_%H%M%S)

docker exec mkat sqlite3 /data/mkat.db ".backup '/data/backup.db'"
docker cp mkat:/data/backup.db "$BACKUP_DIR/mkat_$DATE.db"
docker exec mkat rm /data/backup.db

# Keep last 7 days
find "$BACKUP_DIR" -name "mkat_*.db" -mtime +7 -delete
```

## Upgrading

With the infrastructure stack, **Watchtower handles upgrades automatically**.

For manual upgrades:

```bash
docker compose pull
docker compose up -d
```

## Troubleshooting

### Check logs

```bash
docker logs mkat
docker logs -f mkat  # follow
```

### Database issues

```bash
docker exec mkat sqlite3 /data/mkat.db "PRAGMA integrity_check;"
docker exec mkat sqlite3 /data/mkat.db "VACUUM;"
```

See also: [Environment Variables](/environment-variables) for all configuration options.
