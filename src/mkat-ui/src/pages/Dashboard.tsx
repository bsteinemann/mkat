import { useQuery } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { alertsApi } from '../api/alerts';
import { ServiceState } from '../api/types';
import { StateIndicator } from '../components/services/StateIndicator';
import { AlertItem } from '../components/alerts/AlertItem';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function Dashboard() {
  const { data: servicesData } = useQuery({
    queryKey: ['services'],
    queryFn: () => servicesApi.list(1, 100),
    refetchInterval: 30000,
  });

  const { data: alertsData } = useQuery({
    queryKey: ['alerts', 'recent'],
    queryFn: () => alertsApi.list(1, 5),
    refetchInterval: 30000,
  });

  const services = servicesData?.items ?? [];
  const alerts = alertsData?.items ?? [];

  const counts = {
    up: services.filter(s => s.state === ServiceState.Up).length,
    down: services.filter(s => s.state === ServiceState.Down).length,
    paused: services.filter(s => s.state === ServiceState.Paused).length,
    unknown: services.filter(s => s.state === ServiceState.Unknown).length,
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>

      <div className="grid grid-cols-4 gap-4">
        <StatCard state={ServiceState.Up} count={counts.up} />
        <StatCard state={ServiceState.Down} count={counts.down} />
        <StatCard state={ServiceState.Paused} count={counts.paused} />
        <StatCard state={ServiceState.Unknown} count={counts.unknown} />
      </div>

      <Card className="py-0">
        <CardHeader>
          <CardTitle className="text-lg">Recent Alerts</CardTitle>
        </CardHeader>
        <CardContent>
          {alerts.length === 0 ? (
            <p className="text-muted-foreground">No recent alerts</p>
          ) : (
            <div className="space-y-3">
              {alerts.map(alert => (
                <AlertItem key={alert.id} alert={alert} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {counts.down > 0 && (
        <Card className="bg-red-50 dark:bg-red-950 border-red-200 dark:border-red-800 py-0">
          <CardHeader>
            <CardTitle className="text-lg text-red-800 dark:text-red-200">
              Services Down ({counts.down})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {services
                .filter(s => s.state === ServiceState.Down)
                .map(s => (
                  <div key={s.id} className="flex items-center justify-between">
                    <span className="font-medium">{s.name}</span>
                    <StateIndicator state={s.state} size="sm" />
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function StatCard({ state, count }: { state: ServiceState; count: number }) {
  const labels = {
    [ServiceState.Up]: 'Up',
    [ServiceState.Down]: 'Down',
    [ServiceState.Paused]: 'Paused',
    [ServiceState.Unknown]: 'Unknown',
  };

  return (
    <Card className="py-0">
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <StateIndicator state={state} />
          <span className="text-3xl font-bold">{count}</span>
        </div>
        <p className="text-sm text-muted-foreground mt-2">{labels[state]} Services</p>
      </CardContent>
    </Card>
  );
}
