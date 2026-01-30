import type { MonitorRollup } from '@/api/types';

interface RollupStatsTableProps {
  rollup: MonitorRollup | null | undefined;
}

function formatValue(value: number | null | undefined): string {
  if (value == null) return '—';
  return value.toFixed(2);
}

export function RollupStatsTable({ rollup }: RollupStatsTableProps) {
  if (!rollup) {
    return <div className="text-sm text-muted-foreground">No rollup statistics available</div>;
  }

  const stats = [
    { label: 'Min', value: formatValue(rollup.min) },
    { label: 'Max', value: formatValue(rollup.max) },
    { label: 'Mean', value: formatValue(rollup.mean) },
    { label: 'Median', value: formatValue(rollup.median) },
    { label: 'P80', value: formatValue(rollup.p80) },
    { label: 'P90', value: formatValue(rollup.p90) },
    { label: 'P95', value: formatValue(rollup.p95) },
    { label: 'Std Dev', value: formatValue(rollup.stdDev) },
  ];

  return (
    <div className="grid grid-cols-4 gap-3 text-sm">
      {stats.map((s) => (
        <div key={s.label} className="text-center">
          <div className="text-muted-foreground text-xs">{s.label}</div>
          <div className="font-mono font-medium">{s.value}</div>
        </div>
      ))}
    </div>
  );
}
