# Telegram Bot Setup

## Creating the Bot

1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot`
3. Follow the prompts:
   - Name: `mkat` (or your preferred name)
   - Username: `your_mkat_bot` (must be unique)
4. Save the bot token (looks like `123456789:ABCdefGhIJKlmNoPQRsTUVwxyz`)

## Getting Your Chat ID

### Option 1: Personal Chat

1. Send a message to your new bot
2. Visit: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
3. Look for `"chat":{"id":123456789` in the response
4. The number is your chat ID

### Option 2: Group Chat

1. Add the bot to your group
2. Send a message in the group mentioning the bot
3. Visit: `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
4. Look for the group chat ID (negative number for groups)

### Option 3: Using @userinfobot

1. Forward a message from your target chat to @userinfobot
2. It will reply with the chat ID

## Configuration

Set these environment variables:

```bash
MKAT_TELEGRAM_BOT_TOKEN=123456789:ABCdefGhIJKlmNoPQRsTUVwxyz
MKAT_TELEGRAM_CHAT_ID=123456789
```

## Available Commands

Once configured, your bot will respond to:

- `/status` - Overview of all services (up/down/paused/unknown counts)
- `/list` - List all services with their current states
- `/mute <service> <duration>` - Mute a service (e.g., `/mute MyAPI 1h`)

Duration formats: `15m`, `1h`, `24h`, `7d`

## Inline Buttons

Alert messages include buttons:

- **Acknowledge** - Mark the alert as seen
- **Mute 15m/1h/24h** - Temporarily suppress alerts for that service

## Alert Format

Alerts are sent as formatted Telegram messages with:

- State indicator (DOWN / RECOVERED)
- Service name
- Severity level (Critical / High / Medium / Low)
- Alert message
- Timestamp
