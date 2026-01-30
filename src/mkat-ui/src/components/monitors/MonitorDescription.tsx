import { MonitorType } from '../../api/types';

const descriptions: Record<MonitorType, { label: string; summary: string; detail: string }> = {
  [MonitorType.Webhook]: {
    label: 'Webhook',
    summary: 'External systems report failures and recoveries by calling dedicated URLs.',
    detail:
      'POST to the fail URL to immediately mark the service as down. POST to the recover URL to mark it back up. No polling — state changes are event-driven. Useful when your CI pipeline, deployment script, or another monitoring tool can send HTTP calls on failure.',
  },
  [MonitorType.Heartbeat]: {
    label: 'Heartbeat',
    summary: 'Your service sends periodic "I\'m alive" pings. Silence triggers an alert.',
    detail:
      'Your service must POST to the heartbeat URL within every interval. If no ping arrives before the interval plus grace period elapses, the service is marked as down. When pings resume, the service recovers automatically. Ideal for cron jobs, background workers, and scheduled tasks.',
  },
  [MonitorType.HealthCheck]: {
    label: 'Health Check',
    summary: 'mkat actively polls an HTTP endpoint on a schedule and alerts on failures.',
    detail:
      'Configure a URL, HTTP method, expected status codes, and optional response body regex. mkat sends the request at each interval. If the status code is unexpected, the body doesn\'t match, the request times out, or the connection fails, the service is marked as down. Recovery is automatic when the next check succeeds.',
  },
  [MonitorType.Metric]: {
    label: 'Metric',
    summary: 'Submit numeric values that are evaluated against min/max thresholds.',
    detail:
      'POST a numeric value to the metric URL. The value is checked against configured min/max bounds using one of four strategies: immediate (single violation triggers), consecutive count (N violations in a row), time window average, or sample count average. Useful for monitoring CPU usage, queue depth, response times, or any numeric indicator.',
  },
};

interface Props {
  type: MonitorType;
  variant?: 'compact' | 'full';
}

export function MonitorDescription({ type, variant = 'compact' }: Props) {
  const desc = descriptions[type];
  if (!desc) return null;

  return (
    <div className="text-sm text-muted-foreground">
      <p>{desc.summary}</p>
      {variant === 'full' && (
        <p className="mt-1 text-muted-foreground/70">{desc.detail}</p>
      )}
    </div>
  );
}
