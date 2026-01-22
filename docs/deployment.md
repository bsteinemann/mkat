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

## Reverse Proxy Setup

### Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name mkat.example.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Traefik

```yaml
http:
  routers:
    mkat:
      rule: "Host(`mkat.example.com`)"
      service: mkat
      tls:
        certResolver: letsencrypt

  services:
    mkat:
      loadBalancer:
        servers:
          - url: "http://mkat:8080"
```

### Caddy

```
mkat.example.com {
    reverse_proxy localhost:8080
}
```

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

1. Pull the new image
2. Stop the container
3. Backup the database
4. Start with new image

```bash
docker pull ghcr.io/bsteinemann/mkat:latest
docker stop mkat
# backup...
docker rm mkat
docker run ... # same as before
```

Or with Docker Compose:

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
