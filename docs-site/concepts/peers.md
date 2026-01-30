# Peer Monitoring

Peer monitoring lets two mkat instances monitor each other. If your primary mkat instance goes down, the peer detects it and can alert you through its own notification channels.

## How It Works

1. **Instance A** generates a pairing token containing its URL, name, and a secret
2. **Instance B** accepts the pairing token
3. Both instances create services and monitors for each other
4. Both send heartbeats to each other at regular intervals (default: 30 seconds)

If either instance stops sending heartbeats, the other detects the failure and raises an alert.

## Pairing

### Generate a token (Instance A)

In the mkat UI, go to **Peers** and click **Generate Pairing Token**. Copy the token.

### Accept pairing (Instance B)

In the mkat UI on a different instance, go to **Peers** and click **Accept Pairing**. Paste the token from Instance A.

Both instances will now show each other in their Peers list and create corresponding services.

## Unpairing

Removing a peer deletes the local service and sends a best-effort notification to the remote instance to clean up its side too.

## Requirements

- Both instances must be reachable by each other over the network
- No authentication is needed between peers (token-based security)
- Each peer is represented as a regular service with heartbeat + webhook monitors
