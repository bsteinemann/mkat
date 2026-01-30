import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
} from 'recharts';
import type { Monitor, MonitorEvent, MonitorRollup } from '@/api/types';

interface MetricHistoryChartProps {
  monitor: Monitor;
  events?: MonitorEvent[];
  rollups?: MonitorRollup[];
  dataSource: 'events' | 'hourly' | 'daily';
}

export function MetricHistoryChart({
  monitor,
  events,
  rollups,
  dataSource,
}: MetricHistoryChartProps) {
  const data =
    dataSource === 'events'
      ? (events ?? [])
          .filter((e) => e.value != null)
          .map((e) => ({
            time: new Date(e.createdAt).getTime(),
            value: e.value!,
            outOfRange: e.isOutOfRange,
          }))
          .reverse()
      : (rollups ?? []).map((r) => ({
          time: new Date(r.periodStart).getTime(),
          value: r.mean ?? 0,
          min: r.min,
          max: r.max,
        }));

  if (data.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
        No metric data in this time range
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
          tick={{ fontSize: 11 }}
        />
        <YAxis tick={{ fontSize: 11 }} />
        <Tooltip
          labelFormatter={(v) => new Date(v as number).toLocaleString()}
          formatter={(value) => [
            (value as number).toFixed(2),
            dataSource === 'events' ? 'Value' : 'Mean',
          ]}
          contentStyle={{
            backgroundColor: 'var(--popover)',
            border: '1px solid var(--border)',
            borderRadius: '6px',
            fontSize: '12px',
          }}
        />
        {monitor.minValue != null && (
          <ReferenceLine
            y={monitor.minValue}
            stroke="var(--destructive)"
            strokeDasharray="4 4"
            label={{ value: 'Min', position: 'left', fontSize: 10, fill: 'var(--destructive)' }}
          />
        )}
        {monitor.maxValue != null && (
          <ReferenceLine
            y={monitor.maxValue}
            stroke="var(--destructive)"
            strokeDasharray="4 4"
            label={{ value: 'Max', position: 'left', fontSize: 10, fill: 'var(--destructive)' }}
          />
        )}
        <Line type="monotone" dataKey="value" stroke="var(--chart-4)" strokeWidth={2} dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
