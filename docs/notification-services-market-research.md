# Notification Services Market Research

Research into alternatives to Telegram for mkat notifications.

## Push Notification Services

### 1. ntfy (Self-hosted, HTTP pub-sub)

| Metric | Details |
|--------|---------|
| **Cost** | Free, open-source (or free public instance at ntfy.sh) |
| **Setup** | Very easy - Docker container, pub/sub via simple HTTP PUT/POST |
| **Security** | ACL with fine-grained permissions, supports auth tokens |
| **Mobile apps** | Android (F-Droid/Play), iOS |
| **Self-hostable** | Yes |
| **Integration** | Simple REST API - `curl -d "msg" ntfy.sh/topic` |
| **Verdict** | Best overall alternative for homelab. Minimal resource usage, simple API, great mobile apps. |

### 2. Gotify (Self-hosted, WebSocket-based)

| Metric | Details |
|--------|---------|
| **Cost** | Free, open-source |
| **Setup** | Easy - single Go binary or Docker, web UI included |
| **Security** | Application tokens, basic user management |
| **Mobile apps** | Android only (no iOS) |
| **Self-hostable** | Yes |
| **Integration** | REST API with priority levels |
| **Verdict** | Good if Android-only is acceptable. Simpler auth model than ntfy. |

### 3. Apprise (Multi-service relay)

| Metric | Details |
|--------|---------|
| **Cost** | Free, open-source |
| **Setup** | Medium - Python library/Docker, config for each target service |
| **Security** | Depends on underlying services |
| **Mobile apps** | N/A (relays to 110+ services) |
| **Self-hostable** | Yes |
| **Integration** | Single API that fans out to Telegram, Discord, Slack, email, etc. |
| **Verdict** | Best if you want multi-channel delivery from a single integration point. Not a destination itself. |

### 4. Pushover (Managed push service)

| Metric | Details |
|--------|---------|
| **Cost** | One-time ~$5 per platform (iOS/Android/Desktop), 10k msgs/month |
| **Setup** | Very easy - get API key, POST to API |
| **Security** | User + app token auth, encrypted delivery |
| **Mobile apps** | Android, iOS, Desktop |
| **Self-hostable** | No |
| **Integration** | Simple REST API, supports priority levels, quiet hours, acknowledgments |
| **Verdict** | Polished UX, priority/acknowledgment features ideal for alerting. Small one-time cost. |

## Chat Platform Webhooks

### 5. Discord Webhooks

| Metric | Details |
|--------|---------|
| **Cost** | Free |
| **Setup** | Trivial - create webhook URL in server settings, POST JSON |
| **Security** | URL-based auth (anyone with URL can post). No read access. |
| **Mobile apps** | Discord app (already installed for many users) |
| **Self-hostable** | No (Discord is the platform) |
| **Integration** | Simple POST with JSON body, supports embeds/rich formatting |
| **Rate limit** | 30 requests/minute |
| **Verdict** | Zero cost, near-zero setup. Good if you already use Discord. |

### 6. Slack Webhooks

| Metric | Details |
|--------|---------|
| **Cost** | Free tier available; paid plans ~$8.75/user/month |
| **Setup** | Easy - create Slack app, enable incoming webhooks |
| **Security** | OAuth-based app permissions, workspace-scoped |
| **Mobile apps** | Slack app |
| **Self-hostable** | No |
| **Integration** | POST JSON with Block Kit for rich formatting |
| **Verdict** | Good for teams already on Slack. Overkill for personal homelab. |

### 7. Matrix (via webhook or bot)

| Metric | Details |
|--------|---------|
| **Cost** | Free, open-source protocol |
| **Setup** | Medium - need Matrix server or use matrix.org, create bot account |
| **Security** | End-to-end encryption available, federated |
| **Mobile apps** | Element (Android/iOS) |
| **Self-hostable** | Yes (Synapse/Dendrite server) |
| **Integration** | Matrix client SDK or simple webhook bridges |
| **Verdict** | Good for privacy-focused users. More complex setup than ntfy. |

## SMS

### Providers & Pricing

| Provider | Cost/Message (US) | Free Tier | Setup Difficulty |
|----------|-------------------|-----------|-----------------|
| **Twilio** | $0.0075-0.0083 | Trial credits | Easy - great SDK, docs |
| **Vonage** | $0.0070 | Trial credits | Easy |
| **AWS SNS** | ~$0.0065 | 100 free SMS/month (first year) | Medium - AWS account needed |
| **Plivo** | $0.0055 | Trial credits | Easy |
| **Bird** | $0.0033 | Trial credits | Easy |

### Additional Costs

- **Phone number rental**: ~$1-2/month for a sending number
- **Carrier registration fees**: US requires A2P 10DLC registration (~$2/month + one-time brand fee)
- **Segments**: Messages over 160 chars are split and billed per segment

### Pros

- Universal reach - works on every phone, no app install needed
- Highest urgency perception (people read SMS immediately)
- Works without internet on the receiving end

### Cons

- Ongoing per-message cost - adds up with many alerts
- Regulatory burden - US requires 10DLC registration for application-to-person messaging
- 160 char limit - detailed alerts get expensive (multi-segment)
- No rich formatting - plain text only
- Deliverability varies - carrier filtering can block automated messages

### Security

- Phone number exposure required
- Messages are unencrypted in transit (SS7 network)
- SIM-swap attacks can redirect messages

### Best For

Critical alerts where the recipient may not have internet or app access.

## Email (SMTP / Transactional API)

### Providers & Pricing

| Provider | Free Tier | Paid Price | Setup |
|----------|-----------|-----------|-------|
| **Amazon SES** | 3,000/month (first year) | $0.10/1,000 emails | Medium |
| **Resend** | 3,000/month | $20/month for 50k | Very easy - modern API |
| **SendGrid** | 100/day (~3,000/month) | $19.95/month for 10k | Easy |
| **Mailgun** | 100/day | $15/month for 10k | Easy |
| **Postmark** | None | $15/month for 10k | Easy |
| **Self-hosted SMTP** | Unlimited | Server cost only | Hard (deliverability) |

### Pros

- Universal - everyone has email
- Rich content (HTML, images, links)
- No app install required
- Free or near-free at homelab volumes
- Good audit trail / searchable history
- Attachments possible (logs, reports)

### Cons

- Latency - not real-time, can be delayed minutes
- Spam filters - automated alerts often land in spam/promotions
- Notification fatigue - easily ignored among other emails
- Self-hosted deliverability - running your own SMTP means fighting spam lists, DKIM/SPF/DMARC setup
- No acknowledgment - can't confirm the recipient saw it

### Security

- TLS in transit (STARTTLS)
- DKIM/SPF/DMARC for sender verification
- Well-understood security model

### Setup Complexity for mkat

- **Using a provider (Resend/SES)**: Add API key + recipient email to config, send via HTTP API. Straightforward.
- **Using SMTP directly**: Configure host/port/credentials. .NET has MailKit. Moderate.
- **Self-hosted SMTP**: Need reverse DNS, DKIM keys, warm-up IP reputation. Hard to get right.

## Comparison Summary

### Cost at Different Alert Volumes

| Factor | SMS | Email | Telegram | ntfy | Discord |
|--------|-----|-------|----------|------|---------|
| **100 alerts/month** | ~$1-2 | Free | Free | Free | Free |
| **1000 alerts/month** | ~$7-10 | Free-$0.10 | Free | Free | Free |

### Feature Comparison

| Factor | SMS | Email | Telegram | ntfy | Discord | Pushover |
|--------|-----|-------|----------|------|---------|----------|
| **Setup effort** | Medium | Low-Medium | Low | Low | Trivial | Low |
| **Urgency/visibility** | Highest | Low | High | High | Medium | High |
| **Rich content** | No | Yes (HTML) | Yes (Markdown) | Limited | Yes (embeds) | Limited |
| **Self-hostable** | No | Partially | No | Yes | No | No |
| **Reliability** | High | Medium (spam) | High | High | High | High |
| **Recipient needs** | Phone number | Email address | Telegram account | ntfy app | Discord account | Pushover app |

## Recommendations for mkat

### Priority ranking for implementation

1. **ntfy** - Best single addition. Self-hosted, dead-simple API, great mobile apps, free, matches homelab philosophy.
2. **Email (SMTP)** - Universal fallback. Use a provider like Resend or SES (free tier covers homelab volumes). Everyone has email.
3. **Discord Webhooks** - Zero setup if user already uses Discord. Simple HTTP POST.
4. **SMS (via Twilio/Vonage)** - Optional "critical-only" escalation channel. Per-message cost and registration overhead make it unsuitable as default.
5. **Pushover** - Good UX for alerting with priority/acknowledgment. Small one-time cost.
6. **Slack Webhooks** - For team use cases.
7. **Apprise** - Meta-option: integrate once, deliver to 110+ services. Could be the abstraction layer instead of implementing each individually.

### Architecture Note

Consider implementing a `INotificationChannel` interface in the Application layer, with each provider as a separate Infrastructure implementation. Apprise could serve as a single implementation that covers many providers, reducing the number of integrations to maintain.

## Sources

- [ntfy.sh - Push notifications via PUT/POST](https://ntfy.sh/)
- [Gotify - Simple server for sending and receiving messages](https://gotify.net/)
- [Apprise - 4 reasons to use instead of ntfy or Gotify](https://www.xda-developers.com/reasons-use-apprise-instead-of-ntfy-gotify/)
- [Gotify, Pushover and ntfy comparison](https://debian.ninja/post/2025/09/23/gotify-pushover-and-ntfy-real-time-notifications/)
- [Testing Gotify and Ntfy comparison](https://blog.vezpi.com/en/post/notification-system-gotify-vs-ntfy/)
- [Self-hosted notifications with ntfy and Apprise](https://frasermclean.com/posts/self-hosted-notifications-with-ntfy-and-apprise/)
- [Top 11 SMS providers for developers 2026 - Knock](https://knock.app/blog/the-top-sms-providers-for-developers)
- [US SMS Pricing 2025 Complete Guide](https://www.sent.dm/resources/united-states-sms-pricing)
- [Twilio Pricing](https://www.twilio.com/en-us/pricing)
- [Vonage SMS Pricing](https://www.vonage.com/communications-apis/sms/pricing/)
- [7 Best Transactional Email Services Compared 2026](https://mailtrap.io/blog/transactional-email-services/)
- [Transactional Email APIs Compared 2025](https://www.notificationapi.com/blog/transactional-email-apis)
- [Top transactional email services for developers 2026 - Knock](https://knock.app/blog/the-top-transactional-email-services-for-developers)
- [Top 5 Email Platforms With Free Tiers - Mailgun](https://www.mailgun.com/blog/email/best-free-email-plans/)
