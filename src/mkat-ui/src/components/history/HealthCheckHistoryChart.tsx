import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';
import type { MonitorEvent, MonitorRollup } from '@/api/types';

interface HealthCheckHistoryChartProps {
  events?: MonitorEvent[];
  rollups?: MonitorRollup[];
  dataSource: 'events' | 'hourly' | 'daily';
}

export function HealthCheckHistoryChart({
  events,
  rollups,
  dataSource,
}: HealthCheckHistoryChartProps) {
  const data =
    dataSource === 'events'
      ? (events ?? [])
          .filter((e) => e.value != null)
          .map((e) => ({
            time: new Date(e.createdAt).getTime(),
            value: e.value!,
            success: e.success,
          }))
          .reverse()
      : (rollups ?? []).map((r) => ({
          time: new Date(r.periodStart).getTime(),
          value: r.mean ?? 0,
          min: r.min,
          max: r.max,
          p95: r.p95,
        }));

  if (data.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
        No data available for this time range
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={240}>
      <LineChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
        <XAxis
          dataKey="time"
          type="number"
          domain={['dataMin', 'dataMax']}
          tickFormatter={(v) => {
            const d = new Date(v);
            return d.getHours() + ':' + d.getMinutes().toString().padStart(2, '0');
          }}
          className="text-xs"
          tick={{ fontSize: 11 }}
        />
        <YAxis
          className="text-xs"
          tick={{ fontSize: 11 }}
          tickFormatter={(v) => `${Math.round(v)}ms`}
        />
        <Tooltip
          labelFormatter={(v) => new Date(v as number).toLocaleString()}
          formatter={(value: number) => [`${Math.round(value)}ms`, 'Response Time']}
          contentStyle={{
            backgroundColor: 'var(--popover)',
            border: '1px solid var(--border)',
            borderRadius: '6px',
            fontSize: '12px',
          }}
        />
        {dataSource !== 'events' && (
          <Line
            type="monotone"
            dataKey="p95"
            stroke="var(--chart-5)"
            strokeWidth={1}
            strokeDasharray="4 4"
            dot={false}
            name="P95"
          />
        )}
        <Line
          type="monotone"
          dataKey="value"
          stroke="var(--chart-1)"
          strokeWidth={2}
          dot={false}
          name={dataSource === 'events' ? 'Response Time' : 'Mean'}
        />
      </LineChart>
    </ResponsiveContainer>
  );
}
