---
layout: home
hero:
  name: mkat
  text: Self-hosted monitoring for homelabs
  tagline: Monitor your services with webhooks, heartbeats, health checks, and metrics. Get notified via Telegram when things go wrong.
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/bsteinemann/mkat
features:
  - title: Webhook Monitoring
    details: Services actively report failures and recoveries via simple HTTP calls.
  - title: Heartbeat Monitoring
    details: Expect periodic check-ins from cron jobs, workers, and scheduled tasks.
  - title: Health Check Polling
    details: mkat actively polls your HTTP endpoints and validates responses.
  - title: Metric Tracking
    details: Submit numeric values and alert on threshold violations with configurable strategies.
  - title: Telegram Notifications
    details: Receive alerts with inline buttons to acknowledge or mute directly from Telegram.
  - title: Peer-to-Peer Monitoring
    details: Pair mkat instances to monitor each other for redundancy.
---
