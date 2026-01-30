import type { Alert } from '../../api/types';
import { AlertType, Severity } from '../../api/types';
import { formatDistanceToNow } from 'date-fns';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface Props {
  alert: Alert;
  onAcknowledge?: () => void;
}

const typeLabels: Record<AlertType, string> = {
  [AlertType.Failure]: 'Failure',
  [AlertType.Recovery]: 'Recovery',
  [AlertType.MissedHeartbeat]: 'Missed Heartbeat',
  [AlertType.FailedHealthCheck]: 'Failed Health Check',
};

const typeColors: Record<AlertType, string> = {
  [AlertType.Failure]: 'text-red-600',
  [AlertType.Recovery]: 'text-green-600',
  [AlertType.MissedHeartbeat]: 'text-orange-600',
  [AlertType.FailedHealthCheck]: 'text-red-600',
};

const severityBadge = {
  [Severity.Low]: 'bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300',
  [Severity.Medium]: 'bg-yellow-100 dark:bg-yellow-900 text-yellow-700 dark:text-yellow-300',
  [Severity.High]: 'bg-orange-100 dark:bg-orange-900 text-orange-700 dark:text-orange-300',
  [Severity.Critical]: 'bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300',
};

export function AlertItem({ alert, onAcknowledge }: Props) {
  return (
    <div className="flex items-start justify-between p-3 border rounded">
      <div className="flex-1">
        <div className="flex items-center gap-2">
          <Badge variant="outline" className={typeColors[alert.type]}>
            {typeLabels[alert.type]}
          </Badge>
          <Badge className={severityBadge[alert.severity]}>
            {Severity[alert.severity]}
          </Badge>
        </div>
        <p className="text-sm text-foreground mt-1">{alert.message}</p>
        <p className="text-xs text-muted-foreground mt-1">
          {formatDistanceToNow(new Date(alert.createdAt))} ago
          {alert.acknowledgedAt && ' - Acknowledged'}
        </p>
      </div>
      {onAcknowledge && !alert.acknowledgedAt && (
        <Button
          variant="secondary"
          size="xs"
          onClick={onAcknowledge}
        >
          Ack
        </Button>
      )}
    </div>
  );
}
