# Getting Started

## Running mkat

The quickest way to run mkat is with Docker:

```bash
docker run -d \
  --name mkat \
  -p 8080:8080 \
  -v mkat-data:/data \
  -e MKAT_USERNAME=admin \
  -e MKAT_PASSWORD=changeme \
  ghcr.io/bsteinemann/mkat:latest
```

Open `http://localhost:8080/mkat` and log in with the credentials you set.

### Docker Compose

For a persistent setup, use Docker Compose:

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

volumes:
  mkat-data:
```

```bash
MKAT_PASSWORD=your-password docker compose up -d
```

## Create your first service

Create a service with a heartbeat monitor that expects a check-in every 5 minutes:

```bash
curl -u admin:your-password \
  -X POST http://localhost:8080/mkat/api/v1/services \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Cron Job",
    "description": "Nightly backup script",
    "severity": 2,
    "monitors": [{
      "type": 1,
      "intervalSeconds": 300,
      "gracePeriodSeconds": 60
    }]
  }'
```

The response includes a monitor with a `token`. Save it — you'll use it to send heartbeats.

## Send a heartbeat

From your cron job or script, send a POST request after each successful run:

```bash
curl -X POST http://localhost:8080/mkat/heartbeat/YOUR_TOKEN
```

No authentication is needed for heartbeat endpoints. The service state changes to **UP** after the first heartbeat.

## What happens when a heartbeat is missed

If mkat doesn't receive a heartbeat within the interval + grace period:

1. The service state transitions from **UP** to **DOWN**
2. An alert is created with type `MissedHeartbeat`
3. If you have a notification channel configured, you receive an alert (e.g., Telegram message)

When the next heartbeat arrives, the service recovers to **UP** and a recovery alert is sent.

## Next steps

- [Set up Telegram notifications](/recipes/telegram) to get alerts on your phone
- [Monitor a web API](/recipes/web-api) with health check polling
- [Learn about services](/concepts/services) and how state transitions work
