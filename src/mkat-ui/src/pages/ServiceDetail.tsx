import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { servicesApi } from '../api/services';
import { alertsApi } from '../api/alerts';
import { MonitorType } from '../api/types';
import { StateIndicator } from '../components/services/StateIndicator';
import { CopyableUrl } from '../components/common/CopyableUrl';
import { AlertItem } from '../components/alerts/AlertItem';

export function ServiceDetail() {
  const { serviceId } = useParams({ strict: false }) as { serviceId: string };
  const queryClient = useQueryClient();

  const { data: service, isLoading } = useQuery({
    queryKey: ['services', serviceId],
    queryFn: () => servicesApi.get(serviceId),
  });

  const { data: alertsData } = useQuery({
    queryKey: ['alerts', 'service', serviceId],
    queryFn: () => alertsApi.list(1, 10),
  });

  const pauseMutation = useMutation({
    mutationFn: () => servicesApi.pause(serviceId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const resumeMutation = useMutation({
    mutationFn: () => servicesApi.resume(serviceId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const ackMutation = useMutation({
    mutationFn: (alertId: string) => alertsApi.acknowledge(alertId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const webhookFailMutation = useMutation({
    mutationFn: (url: string) => fetch(url, { method: 'POST' }).then(r => {
      if (!r.ok) throw new Error(`HTTP ${r.status}`);
      return r.json();
    }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const webhookRecoverMutation = useMutation({
    mutationFn: (url: string) => fetch(url, { method: 'POST' }).then(r => {
      if (!r.ok) throw new Error(`HTTP ${r.status}`);
      return r.json();
    }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  if (isLoading || !service) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{service.name}</h1>
          {service.description && (
            <p className="text-gray-500 mt-1">{service.description}</p>
          )}
        </div>
        <div className="flex items-center gap-4">
          <StateIndicator state={service.state} size="lg" />
          <div className="flex gap-2">
            {service.state !== 3 ? (
              <button
                onClick={() => pauseMutation.mutate()}
                className="px-4 py-2 bg-yellow-100 text-yellow-800 rounded hover:bg-yellow-200"
              >
                Pause
              </button>
            ) : (
              <button
                onClick={() => resumeMutation.mutate()}
                className="px-4 py-2 bg-green-100 text-green-800 rounded hover:bg-green-200"
              >
                Resume
              </button>
            )}
            <Link
              to="/services/$serviceId/edit"
              params={{ serviceId }}
              className="px-4 py-2 bg-blue-100 text-blue-800 rounded hover:bg-blue-200"
            >
              Edit
            </Link>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold mb-4">Monitors</h2>
        <div className="space-y-6">
          {service.monitors.map(monitor => (
            <div key={monitor.id} className="border-b pb-4 last:border-0">
              <div className="flex items-center gap-2 mb-3">
                <span className="font-medium">
                  {monitor.type === MonitorType.Webhook ? 'Webhook' : 'Heartbeat'}
                </span>
                <span className="text-sm text-gray-500">
                  ({monitor.intervalSeconds}s interval)
                </span>
              </div>

              {monitor.type === MonitorType.Webhook ? (
                <div className="space-y-3">
                  <CopyableUrl label="Failure URL (HTTP POST)" url={monitor.webhookFailUrl} />
                  <CopyableUrl label="Recovery URL (HTTP POST)" url={monitor.webhookRecoverUrl} />
                  <div className="flex gap-2 mt-2">
                    <button
                      onClick={() => webhookFailMutation.mutate(monitor.webhookFailUrl)}
                      disabled={webhookFailMutation.isPending}
                      className="px-3 py-1.5 text-sm bg-red-100 text-red-800 rounded hover:bg-red-200 disabled:opacity-50"
                    >
                      {webhookFailMutation.isPending ? 'Sending...' : 'Test Fail'}
                    </button>
                    <button
                      onClick={() => webhookRecoverMutation.mutate(monitor.webhookRecoverUrl)}
                      disabled={webhookRecoverMutation.isPending}
                      className="px-3 py-1.5 text-sm bg-green-100 text-green-800 rounded hover:bg-green-200 disabled:opacity-50"
                    >
                      {webhookRecoverMutation.isPending ? 'Sending...' : 'Test Recover'}
                    </button>
                  </div>
                </div>
              ) : (
                <CopyableUrl label="Heartbeat URL" url={monitor.heartbeatUrl} />
              )}

              {monitor.lastCheckIn && (
                <p className="text-sm text-gray-500 mt-2">
                  Last check-in: {new Date(monitor.lastCheckIn).toLocaleString()}
                </p>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold mb-4">Recent Alerts</h2>
        {alertsData?.items.length === 0 ? (
          <p className="text-gray-500">No alerts for this service</p>
        ) : (
          <div className="space-y-3">
            {alertsData?.items.map(alert => (
              <AlertItem
                key={alert.id}
                alert={alert}
                onAcknowledge={() => ackMutation.mutate(alert.id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
