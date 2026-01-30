# Monitor a Web API

Use a **health check monitor** to have mkat poll your API endpoint and alert you when it's unreachable or returning errors.

## Create the service

```bash
curl -u admin:password \
  -X POST http://localhost:8080/mkat/api/v1/services \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Production API",
    "description": "Main backend API",
    "severity": 2,
    "monitors": [{
      "type": 2,
      "healthCheckUrl": "https://api.example.com/health",
      "httpMethod": "GET",
      "expectedStatusCodes": [200],
      "timeoutSeconds": 10,
      "intervalSeconds": 60,
      "gracePeriodSeconds": 120
    }]
  }'
```

This tells mkat to:
- Poll `https://api.example.com/health` every 60 seconds
- Expect a `200` response within 10 seconds
- Wait for 2 minutes of failures before alerting

## Body matching

You can also validate the response body with a regex:

```json
{
  "type": 2,
  "healthCheckUrl": "https://api.example.com/health",
  "httpMethod": "GET",
  "expectedStatusCodes": [200],
  "bodyMatchRegex": "\"status\":\\s*\"healthy\"",
  "timeoutSeconds": 10,
  "intervalSeconds": 60,
  "gracePeriodSeconds": 120
}
```

## Alternative: Webhook monitoring

If your API can't be polled (e.g., it's behind a firewall), use **webhook monitoring** instead. Have your API call mkat when it detects a problem:

```bash
# In your API's error handler:
curl -X POST http://mkat-host/mkat/webhook/{token}/fail

# In your API's recovery logic:
curl -X POST http://mkat-host/mkat/webhook/{token}/recover
```

See [Monitor types](/concepts/monitors) for details on each approach.
