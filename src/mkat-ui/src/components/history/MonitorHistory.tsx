import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Monitor } from '@/api/types';
import { MonitorType, Granularity } from '@/api/types';
import { monitorEventsApi, monitorRollupsApi } from '@/api/services';
import { TimeRangeSelector } from './TimeRangeSelector';
import { getTimeRangeDate, getDataSource } from './timeRangeUtils';
import type { TimeRange } from './timeRangeUtils';
import { HealthCheckHistoryChart } from './HealthCheckHistoryChart';
import { HeartbeatHistoryChart } from './HeartbeatHistoryChart';
import { WebhookHistoryChart } from './WebhookHistoryChart';
import { MetricHistoryChart } from './MetricHistoryChart';
import { UptimeBadge } from './UptimeBadge';
import { RollupStatsTable } from './RollupStatsTable';

interface MonitorHistoryProps {
  monitor: Monitor;
}

export function MonitorHistory({ monitor }: MonitorHistoryProps) {
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const dataSource = getDataSource(timeRange);
  const from = getTimeRangeDate(timeRange).toISOString();
  const to = new Date().toISOString();

  const granularity = dataSource === 'hourly' ? Granularity.Hourly : Granularity.Daily;

  const { data: events } = useQuery({
    queryKey: ['monitor-events', monitor.id, timeRange],
    queryFn: () => monitorEventsApi.getByMonitor(monitor.id, { from, to, limit: 1000 }),
    enabled: dataSource === 'events',
  });

  const { data: rollups } = useQuery({
    queryKey: ['monitor-rollups', monitor.id, timeRange, granularity],
    queryFn: () =>
      monitorRollupsApi.getByMonitor(monitor.id, {
        granularity,
        from,
        to,
      }),
    enabled: dataSource !== 'events',
  });

  // Compute uptime from current data
  const uptimePercent = (() => {
    if (dataSource === 'events' && events) {
      const total = events.length;
      if (total === 0) return null;
      const success = events.filter((e) => e.success).length;
      return (success / total) * 100;
    }
    if (dataSource !== 'events' && rollups && rollups.length > 0) {
      const totalCount = rollups.reduce((s, r) => s + r.count, 0);
      const totalSuccess = rollups.reduce((s, r) => s + r.successCount, 0);
      if (totalCount === 0) return null;
      return (totalSuccess / totalCount) * 100;
    }
    return null;
  })();

  // Latest rollup for stats table
  const latestRollup = rollups && rollups.length > 0 ? rollups[rollups.length - 1] : null;

  return (
    <div className="space-y-3 mt-4 pt-4 border-t">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-muted-foreground">History</span>
          <UptimeBadge percent={uptimePercent} />
        </div>
        <TimeRangeSelector value={timeRange} onChange={setTimeRange} />
      </div>

      {monitor.type === MonitorType.HealthCheck && (
        <HealthCheckHistoryChart events={events} rollups={rollups} dataSource={dataSource} />
      )}

      {monitor.type === MonitorType.Heartbeat && (
        <HeartbeatHistoryChart events={events} rollups={rollups} dataSource={dataSource} />
      )}

      {monitor.type === MonitorType.Webhook && (
        <WebhookHistoryChart events={events} rollups={rollups} dataSource={dataSource} />
      )}

      {monitor.type === MonitorType.Metric && (
        <MetricHistoryChart
          monitor={monitor}
          events={events}
          rollups={rollups}
          dataSource={dataSource}
        />
      )}

      {dataSource !== 'events' && latestRollup && <RollupStatsTable rollup={latestRollup} />}
    </div>
  );
}
