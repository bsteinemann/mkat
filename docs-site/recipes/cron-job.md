# Monitor a Cron Job

Use a **heartbeat monitor** to ensure your cron job runs on schedule. If a heartbeat is missed, mkat alerts you.

## Create the service

```bash
curl -u admin:password \
  -X POST http://localhost:8080/mkat/api/v1/services \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nightly Backup",
    "description": "Database backup runs at 2 AM",
    "severity": 2,
    "monitors": [{
      "type": 1,
      "intervalSeconds": 86400,
      "gracePeriodSeconds": 3600
    }]
  }'
```

This expects a heartbeat every 24 hours (86400s) with a 1-hour grace period (3600s).

Save the `token` from the response.

## Add the heartbeat to your cron job

At the end of your script, send a heartbeat:

```bash
#!/bin/bash
# backup.sh

pg_dump mydb > /backups/mydb_$(date +%Y%m%d).sql

# Tell mkat the job completed
curl -sf -X POST http://mkat-host/mkat/heartbeat/YOUR_TOKEN
```

### Only on success

Make the heartbeat conditional so mkat alerts you on failures:

```bash
#!/bin/bash
pg_dump mydb > /backups/mydb_$(date +%Y%m%d).sql

if [ $? -eq 0 ]; then
  curl -sf -X POST http://mkat-host/mkat/heartbeat/YOUR_TOKEN
fi
```

## Common intervals

| Schedule | Interval | Grace Period |
|----------|----------|-------------|
| Every 5 minutes | 300 | 60 |
| Hourly | 3600 | 600 |
| Daily | 86400 | 3600 |
| Weekly | 604800 | 86400 |

Set the grace period based on how long the job normally takes plus some buffer.
