# Set Up Telegram Notifications

Receive alerts on your phone via Telegram with inline buttons to acknowledge or mute.

## Create a Telegram bot

1. Open Telegram and message [@BotFather](https://t.me/botfather)
2. Send `/newbot`
3. Follow the prompts to set a name and username
4. Save the bot token (looks like `123456789:ABCdefGhIJKlmNoPQRsTUVwxyz`)

## Get your chat ID

### Option 1: @userinfobot (easiest)

1. Message [@userinfobot](https://t.me/userinfobot) on Telegram
2. It replies with your chat ID

### Option 2: Bot API

1. Send `/start` to your new bot
2. Open `https://api.telegram.org/bot<YOUR_TOKEN>/getUpdates`
3. Find `"chat":{"id":123456789}` in the response

### Group chat

1. Add the bot to your group
2. Send a message mentioning the bot
3. Check `getUpdates` — group IDs are negative numbers

## Configure mkat

### Option A: Environment variables (global default)

```bash
MKAT_TELEGRAM_BOT_TOKEN=123456789:ABCdefGhIJKlmNoPQRsTUVwxyz
MKAT_TELEGRAM_CHAT_ID=123456789
```

### Option B: Per-contact channel (via API)

Create a contact and add a Telegram channel:

```bash
# Create contact
curl -u admin:password -X POST http://localhost:8080/mkat/api/v1/contacts \
  -H "Content-Type: application/json" \
  -d '{"name": "On-Call"}'

# Add Telegram channel (use the contact ID from above)
curl -u admin:password \
  -X POST http://localhost:8080/mkat/api/v1/contacts/{contactId}/channels \
  -H "Content-Type: application/json" \
  -d '{
    "type": 0,
    "configuration": "{\"BotToken\":\"YOUR_TOKEN\",\"ChatId\":\"YOUR_CHAT_ID\"}"
  }'

# Test it
curl -u admin:password \
  -X POST http://localhost:8080/mkat/api/v1/contacts/{contactId}/channels/{channelId}/test
```

## Bot commands

Once configured, your bot responds to:

| Command | Description |
|---------|-------------|
| `/status` | Overview of all services (up/down/paused/unknown counts) |
| `/list` | List all services with their current states |
| `/mute <service> <duration>` | Mute a service (e.g., `/mute MyAPI 1h`) |

Duration formats: `15m`, `1h`, `24h`, `7d`

## Inline buttons

Alert messages include action buttons:

- **Acknowledge** — Mark the alert as seen
- **Mute 15m / 1h / 24h** — Temporarily suppress alerts for that service
