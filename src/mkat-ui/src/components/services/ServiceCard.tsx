import { Link } from '@tanstack/react-router';
import type { Service } from '../../api/types';
import { ServiceState, Severity } from '../../api/types';
import { StateIndicator } from './StateIndicator';
import { formatDistanceToNow } from 'date-fns';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

interface Props {
  service: Service;
  onPause?: () => void;
  onResume?: () => void;
}

const severityBadge = {
  [Severity.Low]: { label: 'Low', variant: 'outline' as const, className: '' },
  [Severity.Medium]: {
    label: 'Medium',
    variant: 'secondary' as const,
    className: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  },
  [Severity.High]: {
    label: 'High',
    variant: 'secondary' as const,
    className: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200',
  },
  [Severity.Critical]: { label: 'Critical', variant: 'destructive' as const, className: '' },
};

export function ServiceCard({ service, onPause, onResume }: Props) {
  const severity = severityBadge[service.severity];

  return (
    <Card className="py-0">
      <CardContent className="p-4">
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2">
              <Link
                to="/services/$serviceId"
                params={{ serviceId: service.id }}
                className="text-lg font-semibold text-foreground hover:text-blue-600 dark:hover:text-blue-400"
              >
                {service.name}
              </Link>
              <Badge variant={severity.variant} className={severity.className}>
                {severity.label}
              </Badge>
            </div>
            {service.description && (
              <p className="text-sm text-muted-foreground mt-1">{service.description}</p>
            )}
          </div>
          <span className={service.isSuppressed ? 'opacity-50' : ''}>
            <StateIndicator state={service.state} />
          </span>
        </div>

        {service.isSuppressed && service.suppressionReason && (
          <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">
            Suppressed — {service.suppressionReason}
          </p>
        )}

        <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
          <span>Updated {formatDistanceToNow(new Date(service.updatedAt))} ago</span>
          <div className="flex gap-2">
            {service.state !== ServiceState.Paused ? (
              <Button
                variant="ghost"
                size="sm"
                className="p-0 h-auto text-yellow-600 dark:text-yellow-400 hover:text-yellow-800 dark:hover:text-yellow-300 hover:bg-transparent"
                onClick={onPause}
              >
                Pause
              </Button>
            ) : (
              <Button
                variant="ghost"
                size="sm"
                className="p-0 h-auto text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 hover:bg-transparent"
                onClick={onResume}
              >
                Resume
              </Button>
            )}
            <Link
              to="/services/$serviceId/edit"
              params={{ serviceId: service.id }}
              className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300"
            >
              Edit
            </Link>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
