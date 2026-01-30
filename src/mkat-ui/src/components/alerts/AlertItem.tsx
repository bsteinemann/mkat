import type { Alert } from '../../api/types';
import { AlertType, Severity } from '../../api/types';
import { formatDistanceToNow } from 'date-fns';
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
  [Severity.Low]: 'bg-green-100 text-green-700',
  [Severity.Medium]: 'bg-yellow-100 text-yellow-700',
  [Severity.High]: 'bg-orange-100 text-orange-700',
  [Severity.Critical]: 'bg-red-100 text-red-700',
};

export function AlertItem({ alert, onAcknowledge }: Props) {
  return (
    <div className="flex items-start justify-between p-3 border rounded">
      <div className="flex-1">
        <div className="flex items-center gap-2">
          <span className={`text-sm font-medium ${typeColors[alert.type]}`}>
            {typeLabels[alert.type]}
          </span>
          <span className={`text-xs px-2 py-0.5 rounded ${severityBadge[alert.severity]}`}>
            {Severity[alert.severity]}
          </span>
        </div>
        <p className="text-sm text-gray-700 mt-1">{alert.message}</p>
        <p className="text-xs text-gray-500 mt-1">
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
