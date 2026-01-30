import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { servicesApi } from '../api/services';
import { alertsApi } from '../api/alerts';
import { MonitorType, ThresholdStrategy } from '../api/types';
import { StateIndicator } from '../components/services/StateIndicator';
import { CopyableUrl } from '../components/common/CopyableUrl';
import { AlertItem } from '../components/alerts/AlertItem';
import { MonitorDescription } from '../components/monitors/MonitorDescription';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';

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
    mutationFn: (url: string) =>
      fetch(url, { method: 'POST' }).then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const webhookRecoverMutation = useMutation({
    mutationFn: (url: string) =>
      fetch(url, { method: 'POST' }).then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  if (isLoading || !service)
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div className="space-y-2">
            <Skeleton className="h-8 w-48" />
            <Skeleton className="h-4 w-64" />
          </div>
          <div className="flex items-center gap-4">
            <Skeleton className="h-8 w-16 rounded-full" />
            <Skeleton className="h-9 w-20 rounded-md" />
            <Skeleton className="h-9 w-16 rounded-md" />
          </div>
        </div>
        <Skeleton className="h-48 w-full rounded-lg" />
        <Skeleton className="h-36 w-full rounded-lg" />
      </div>
    );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{service.name}</h1>
          {service.description && (
            <p className="text-muted-foreground mt-1">{service.description}</p>
          )}
        </div>
        <div className="flex items-center gap-4">
          <StateIndicator state={service.state} size="lg" />
          <div className="flex gap-2">
            {service.state !== 3 ? (
              <Button
                variant="outline"
                className="bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200 hover:bg-yellow-200 dark:hover:bg-yellow-800 border-yellow-200 dark:border-yellow-700"
                onClick={() => pauseMutation.mutate()}
              >
                Pause
              </Button>
            ) : (
              <Button
                variant="outline"
                className="bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200 hover:bg-green-200 dark:hover:bg-green-800 border-green-200 dark:border-green-700"
                onClick={() => resumeMutation.mutate()}
              >
                Resume
              </Button>
            )}
            <Link
              to="/services/$serviceId/edit"
              params={{ serviceId }}
              className="px-4 py-2 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 rounded hover:bg-blue-200 dark:hover:bg-blue-800"
            >
              Edit
            </Link>
          </div>
        </div>
      </div>

      <Card className="py-0">
        <CardHeader>
          <CardTitle className="text-lg">Monitors</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-6">
            {service.monitors.map((monitor) => (
              <div key={monitor.id} className="border-b pb-4 last:border-0">
                <div className="flex items-center gap-2 mb-3">
                  <span className="font-medium">
                    {monitor.type === MonitorType.Webhook && 'Webhook'}
                    {monitor.type === MonitorType.Heartbeat && 'Heartbeat'}
                    {monitor.type === MonitorType.HealthCheck && 'Health Check'}
                    {monitor.type === MonitorType.Metric && 'Metric'}
                  </span>
                  <span className="text-sm text-muted-foreground">
                    ({monitor.intervalSeconds}s interval)
                  </span>
                </div>

                <MonitorDescription type={monitor.type} variant="full" />

                {monitor.type === MonitorType.Webhook && (
                  <div className="space-y-3">
                    <CopyableUrl label="Failure URL (HTTP POST)" url={monitor.webhookFailUrl} />
                    <CopyableUrl label="Recovery URL (HTTP POST)" url={monitor.webhookRecoverUrl} />
                    <div className="flex gap-2 mt-2">
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => webhookFailMutation.mutate(monitor.webhookFailUrl)}
                        disabled={webhookFailMutation.isPending}
                      >
                        {webhookFailMutation.isPending ? 'Sending...' : 'Test Fail'}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200 hover:bg-green-200 dark:hover:bg-green-800 border-green-200 dark:border-green-700"
                        onClick={() => webhookRecoverMutation.mutate(monitor.webhookRecoverUrl)}
                        disabled={webhookRecoverMutation.isPending}
                      >
                        {webhookRecoverMutation.isPending ? 'Sending...' : 'Test Recover'}
                      </Button>
                    </div>
                  </div>
                )}

                {monitor.type === MonitorType.Heartbeat && (
                  <CopyableUrl label="Heartbeat URL" url={monitor.heartbeatUrl} />
                )}

                {monitor.type === MonitorType.Metric && (
                  <div className="space-y-3">
                    <CopyableUrl
                      label="Metric Push URL (POST with value)"
                      url={monitor.metricUrl}
                    />
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm text-muted-foreground mt-2">
                      {monitor.minValue != null && <span>Min: {monitor.minValue}</span>}
                      {monitor.maxValue != null && <span>Max: {monitor.maxValue}</span>}
                      {monitor.thresholdStrategy != null && (
                        <span>Strategy: {ThresholdStrategy[monitor.thresholdStrategy]}</span>
                      )}
                      {monitor.thresholdCount != null && (
                        <span>Count: {monitor.thresholdCount}</span>
                      )}
                      {monitor.retentionDays != null && (
                        <span>Retention: {monitor.retentionDays}d</span>
                      )}
                    </div>
                    {monitor.lastMetricValue != null && monitor.lastMetricAt && (
                      <p className="text-sm text-muted-foreground mt-2">
                        Latest: {monitor.lastMetricValue} at{' '}
                        {new Date(monitor.lastMetricAt).toLocaleString()}
                      </p>
                    )}
                  </div>
                )}

                {monitor.type === MonitorType.HealthCheck && (
                  <div className="space-y-3">
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm text-muted-foreground">
                      <span>URL: {monitor.healthCheckUrl}</span>
                      <span>Method: {monitor.httpMethod ?? 'GET'}</span>
                      <span>Expected: {monitor.expectedStatusCodes ?? '200'}</span>
                      <span>Timeout: {monitor.timeoutSeconds ?? 10}s</span>
                      {monitor.bodyMatchRegex && (
                        <span className="col-span-2">
                          Body match:{' '}
                          <code className="bg-muted px-1 rounded">{monitor.bodyMatchRegex}</code>
                        </span>
                      )}
                    </div>
                  </div>
                )}

                {monitor.lastCheckIn && (
                  <p className="text-sm text-muted-foreground mt-2">
                    Last check-in: {new Date(monitor.lastCheckIn).toLocaleString()}
                  </p>
                )}
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card className="py-0">
        <CardHeader>
          <CardTitle className="text-lg">Recent Alerts</CardTitle>
        </CardHeader>
        <CardContent>
          {alertsData?.items.length === 0 ? (
            <p className="text-muted-foreground">No alerts for this service</p>
          ) : (
            <div className="space-y-3">
              {alertsData?.items.map((alert) => (
                <AlertItem
                  key={alert.id}
                  alert={alert}
                  onAcknowledge={() => ackMutation.mutate(alert.id)}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
