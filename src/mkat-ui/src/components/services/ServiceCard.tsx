import { Link } from '@tanstack/react-router';
import type { Service } from '../../api/types';
import { ServiceState, Severity } from '../../api/types';
import { StateIndicator } from './StateIndicator';
import { formatDistanceToNow } from 'date-fns';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

interface Props {
  service: Service;
  onPause?: () => void;
  onResume?: () => void;
}

const severityColors = {
  [Severity.Low]: 'border-green-200 dark:border-green-800',
  [Severity.Medium]: 'border-yellow-200 dark:border-yellow-800',
  [Severity.High]: 'border-orange-200 dark:border-orange-800',
  [Severity.Critical]: 'border-red-200 dark:border-red-800',
};

export function ServiceCard({ service, onPause, onResume }: Props) {
  return (
    <Card className={`border-l-4 ${severityColors[service.severity]} py-0`}>
      <CardContent className="p-4">
        <div className="flex items-start justify-between">
          <div>
            <Link
              to="/services/$serviceId"
              params={{ serviceId: service.id }}
              className="text-lg font-semibold text-foreground hover:text-blue-600 dark:hover:text-blue-400"
            >
              {service.name}
            </Link>
            {service.description && (
              <p className="text-sm text-muted-foreground mt-1">{service.description}</p>
            )}
          </div>
          <StateIndicator state={service.state} />
        </div>

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
