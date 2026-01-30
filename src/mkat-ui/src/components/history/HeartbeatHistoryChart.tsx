import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Cell,
} from 'recharts';
import type { MonitorEvent, MonitorRollup } from '@/api/types';

interface HeartbeatHistoryChartProps {
  events?: MonitorEvent[];
  rollups?: MonitorRollup[];
  dataSource: 'events' | 'hourly' | 'daily';
}

export function HeartbeatHistoryChart({ events, rollups, dataSource }: HeartbeatHistoryChartProps) {
  if (dataSource === 'events') {
    const data = (events ?? [])
      .map((e) => ({
        time: new Date(e.createdAt).getTime(),
        value: 1,
        success: e.success,
      }))
      .reverse();

    if (data.length === 0) {
      return (
        <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
          No heartbeats in this time range
        </div>
      );
    }

    return (
      <ResponsiveContainer width="100%" height={160}>
        <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
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
          <YAxis hide />
          <Tooltip
            labelFormatter={(v) => new Date(v as number).toLocaleString()}
            formatter={() => ['Heartbeat', 'Event']}
            contentStyle={{
              backgroundColor: 'var(--popover)',
              border: '1px solid var(--border)',
              borderRadius: '6px',
              fontSize: '12px',
            }}
          />
          <Bar dataKey="value" maxBarSize={8} radius={[2, 2, 0, 0]}>
            {data.map((entry, index) => (
              <Cell key={index} fill={entry.success ? 'var(--chart-2)' : 'var(--destructive)'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    );
  }

  // Rollup view
  const data = (rollups ?? []).map((r) => ({
    time: new Date(r.periodStart).getTime(),
    success: r.successCount,
    failure: r.failureCount,
  }));

  if (data.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
        No data available for this time range
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={160}>
      <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
        <XAxis
          dataKey="time"
          type="number"
          domain={['dataMin', 'dataMax']}
          tickFormatter={(v) =>
            new Date(v).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
          }
          tick={{ fontSize: 11 }}
        />
        <YAxis tick={{ fontSize: 11 }} />
        <Tooltip
          labelFormatter={(v) => new Date(v as number).toLocaleString()}
          contentStyle={{
            backgroundColor: 'var(--popover)',
            border: '1px solid var(--border)',
            borderRadius: '6px',
            fontSize: '12px',
          }}
        />
        <Bar
          dataKey="success"
          stackId="a"
          fill="var(--chart-2)"
          name="Received"
          radius={[0, 0, 0, 0]}
        />
        <Bar
          dataKey="failure"
          stackId="a"
          fill="var(--destructive)"
          name="Missed"
          radius={[2, 2, 0, 0]}
        />
      </BarChart>
    </ResponsiveContainer>
  );
}
