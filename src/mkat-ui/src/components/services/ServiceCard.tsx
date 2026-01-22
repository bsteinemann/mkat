import { Link } from '@tanstack/react-router';
import type { Service } from '../../api/types';
import { ServiceState, Severity } from '../../api/types';
import { StateIndicator } from './StateIndicator';
import { formatDistanceToNow } from 'date-fns';

interface Props {
  service: Service;
  onPause?: () => void;
  onResume?: () => void;
}

const severityColors = {
  [Severity.Low]: 'border-green-200',
  [Severity.Medium]: 'border-yellow-200',
  [Severity.High]: 'border-orange-200',
  [Severity.Critical]: 'border-red-200',
};

export function ServiceCard({ service, onPause, onResume }: Props) {
  return (
    <div className={`bg-white rounded-lg shadow p-4 border-l-4 ${severityColors[service.severity]}`}>
      <div className="flex items-start justify-between">
        <div>
          <Link
            to="/services/$serviceId"
            params={{ serviceId: service.id }}
            className="text-lg font-semibold text-gray-900 hover:text-blue-600"
          >
            {service.name}
          </Link>
          {service.description && (
            <p className="text-sm text-gray-500 mt-1">{service.description}</p>
          )}
        </div>
        <StateIndicator state={service.state} />
      </div>

      <div className="mt-4 flex items-center justify-between text-sm text-gray-500">
        <span>Updated {formatDistanceToNow(new Date(service.updatedAt))} ago</span>
        <div className="flex gap-2">
          {service.state !== ServiceState.Paused ? (
            <button
              onClick={onPause}
              className="text-yellow-600 hover:text-yellow-800"
            >
              Pause
            </button>
          ) : (
            <button
              onClick={onResume}
              className="text-green-600 hover:text-green-800"
            >
              Resume
            </button>
          )}
          <Link
            to="/services/$serviceId/edit"
            params={{ serviceId: service.id }}
            className="text-blue-600 hover:text-blue-800"
          >
            Edit
          </Link>
        </div>
      </div>
    </div>
  );
}
