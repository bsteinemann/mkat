# Milestone 8: Peer Monitoring

**Goal:** Mutual availability monitoring and notification failure detection between mkat instances

**Dependencies:** M3 (Monitoring Engine), M4 (Notifications), M5 (Frontend)

---

## Overview

Peer Monitoring lets two mkat instances monitor each other using mutual heartbeats for availability, plus a webhook for notification health. It reuses existing heartbeat and webhook infrastructure — each peer becomes a regular service on the other instance.

Each instance monitors its own services as usual, but additionally monitors any paired mkat instances as peers.

---

## What Gets Created During Pairing

On each instance, pairing creates:
- A **Service** representing the peer (e.g. "Peer: Home Server")
- A **Heartbeat Monitor** — the peer sends periodic heartbeats to prove it's alive
- A **Webhook Monitor** — the peer calls `/webhook/{token}/fail` if its own notification delivery fails

---

## Domain Model

### New Entity: `Peer`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `Name` | string | Display name for the peer |
| `Url` | string | Base URL of the peer instance |
| `HeartbeatToken` | string | Token to send heartbeats TO the peer |
| `WebhookToken` | string | Token to call /webhook/fail on the peer |
| `ServiceId` | Guid | FK to the local Service created for this peer |
| `PairedAt` | DateTime (UTC) | When the pairing was established |
| `HeartbeatIntervalSeconds` | int | How often to send heartbeats (default: 30) |

### Pairing Token (transient)

Base64-encoded JSON blob, valid for 10 minutes:
```json
{
  "url": "https://instance-a.example.com",
  "secret": "random-one-time-string",
  "expiresAt": "2026-01-23T12:00:00Z",
  "name": "Instance A"
}
```

The secret is stored temporarily until pairing completes, then discarded.

---

## Pairing Flow

1. On Instance A: User calls `POST /api/v1/peers/pair/initiate` → gets a token string
2. User pastes token into Instance B's UI, calls `POST /api/v1/peers/pair/complete` with the token
3. Instance B decodes the token, calls `POST {instanceA_url}/api/v1/peers/pair/accept` with:
   ```json
   {
     "secret": "the-one-time-secret",
     "url": "https://instance-b.example.com",
     "name": "Instance B"
   }
   ```
4. Instance A validates the secret, creates Service + Heartbeat Monitor + Webhook Monitor for B, responds with:
   ```json
   {
     "heartbeatToken": "token-for-b-to-send-heartbeats-to-a",
     "webhookToken": "token-for-b-to-report-notification-failures-to-a",
     "heartbeatIntervalSeconds": 30
   }
   ```
5. Instance B creates its own Service + Monitors for A, stores A's tokens
6. Both instances begin sending heartbeats to each other

**Authentication note:** The `/pair/accept` endpoint is authenticated by the one-time secret in the pairing token, not by Basic Auth.

---

## API Endpoints

### Pairing (authenticated)

```
POST /api/v1/peers/pair/initiate    Generate a pairing token
POST /api/v1/peers/pair/complete    Accept a pairing token and establish connection
```

### Pairing protocol (called between instances, authenticated by secret)

```
POST /api/v1/peers/pair/accept      Called by the completing instance during pairing
```

### Peer management (authenticated)

```
GET    /api/v1/peers                List paired instances
DELETE /api/v1/peers/{id}           Unpair (removes service + monitors + stops heartbeats)
```

---

## Background Workers

| Worker | Interval | Responsibility |
|--------|----------|----------------|
| `PeerHeartbeatWorker` | 10s | Send heartbeats to all paired peers on their configured interval |

For each `Peer` entity, every `HeartbeatIntervalSeconds`:
- Calls `POST {peer.Url}/heartbeat/{peer.HeartbeatToken}`
- HTTP failure is expected when peer is down — the local `HeartbeatMonitorWorker` handles detection of missed inbound heartbeats

---

## Notification Failure Hook

When the existing `AlertDispatchWorker` fails to deliver a notification:
- For each paired peer, call `POST {peer.Url}/webhook/{peer.WebhookToken}/fail`
- When delivery later succeeds, call `POST {peer.Url}/webhook/{peer.WebhookToken}/recover`

This is a small addition to the existing dispatch logic.

---

## Unpair Cleanup

When `DELETE /api/v1/peers/{id}` is called:
- Stop sending heartbeats to that peer
- Delete the local Service + Monitors created for the peer
- Delete the `Peer` entity
- Best-effort call to the peer to clean up its side (non-blocking, tolerates failure)

---

## Frontend

- **Peers page** (`/peers`): List of paired instances with status, last heartbeat time
- **Pair dialog**: "Generate token" or "Enter token" flow
- **Peer detail**: Shows the service, monitors, and unpair option
- **Dashboard**: Peer status indicators alongside regular services

---

## Deliverables

- [ ] `Peer` entity and repository
- [ ] EF Core migration for Peer table
- [ ] Pairing token generation and validation logic
- [ ] Pairing API endpoints (initiate, complete, accept)
- [ ] Peer CRUD endpoints (list, delete/unpair)
- [ ] Auto-creation of Service + Heartbeat Monitor + Webhook Monitor during pairing
- [ ] `PeerHeartbeatWorker` (sends heartbeats to peers)
- [ ] Notification failure hook in `AlertDispatchWorker`
- [ ] Unpair cleanup logic
- [ ] FluentValidation for pairing requests
- [ ] Frontend: peers page, pair dialog, peer status on dashboard
- [ ] Unit tests for pairing logic and token validation
- [ ] Integration tests for pairing flow and worker

---

## Definition of Done

- Two instances can pair via token exchange
- Both send heartbeats and detect peer downtime via existing HeartbeatMonitorWorker
- Notification failures reported to peer via webhook
- Unpairing cleans up both sides (best-effort on remote)
- Frontend shows peer status and pairing flow
- All tests pass

---

**Status:** Draft
**Created:** 2026-01-23
