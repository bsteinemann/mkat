import {
  ResponsiveContainer,
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Cell,
} from 'recharts';
import type { MonitorEvent, MonitorRollup } from '@/api/types';

interface WebhookHistoryChartProps {
  events?: MonitorEvent[];
  rollups?: MonitorRollup[];
  dataSource: 'events' | 'hourly' | 'daily';
}

export function WebhookHistoryChart({
  events,
  rollups,
  dataSource,
}: WebhookHistoryChartProps) {
  if (dataSource === 'events') {
    const data = (events ?? [])
      .map((e) => ({
        time: new Date(e.createdAt).getTime(),
        y: e.success ? 1 : 0,
        success: e.success,
        message: e.message,
      }))
      .reverse();

    if (data.length === 0) {
      return (
        <div className="flex items-center justify-center h-48 text-sm text-muted-foreground">
          No webhook events in this time range
        </div>
      );
    }

    return (
      <ResponsiveContainer width="100%" height={120}>
        <ScatterChart margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
          <XAxis
            dataKey="time"
            type="number"
            domain={['dataMin', 'dataMax']}
            tickFormatter={(v) => new Date(v).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
            tick={{ fontSize: 11 }}
          />
          <YAxis
            dataKey="y"
            type="number"
            domain={[-0.5, 1.5]}
            ticks={[0, 1]}
            tickFormatter={(v) => (v === 1 ? 'Recover' : 'Fail')}
            tick={{ fontSize: 11 }}
            width={55}
          />
          <Tooltip
            labelFormatter={(v) => new Date(v as number).toLocaleString()}
            formatter={(_, _name, props) => {
              const p = props.payload as (typeof data)[0];
              return [p.success ? 'Recovery' : 'Failure', p.message ?? 'Webhook'];
            }}
            contentStyle={{
              backgroundColor: 'var(--popover)',
              border: '1px solid var(--border)',
              borderRadius: '6px',
              fontSize: '12px',
            }}
          />
          <Scatter data={data} dataKey="y">
            {data.map((entry, index) => (
              <Cell
                key={index}
                fill={entry.success ? 'var(--chart-2)' : 'var(--destructive)'}
              />
            ))}
          </Scatter>
        </ScatterChart>
      </ResponsiveContainer>
    );
  }

  // Rollup fallback — same as heartbeat bar chart
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
    <ResponsiveContainer width="100%" height={120}>
      <ScatterChart margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
        <XAxis
          dataKey="time"
          type="number"
          domain={['dataMin', 'dataMax']}
          tickFormatter={(v) => new Date(v).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
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
        <Scatter data={data.map((d) => ({ ...d, y: d.success }))} dataKey="y" fill="var(--chart-2)" name="Recoveries" />
      </ScatterChart>
    </ResponsiveContainer>
  );
}
