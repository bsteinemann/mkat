import { Button } from '@/components/ui/button';

export type TimeRange = '1h' | '6h' | '24h' | '7d' | '30d' | '90d' | '1y';

interface TimeRangeSelectorProps {
  value: TimeRange;
  onChange: (range: TimeRange) => void;
}

const ranges: { label: string; value: TimeRange }[] = [
  { label: '1h', value: '1h' },
  { label: '6h', value: '6h' },
  { label: '24h', value: '24h' },
  { label: '7d', value: '7d' },
  { label: '30d', value: '30d' },
  { label: '90d', value: '90d' },
  { label: '1y', value: '1y' },
];

export function getTimeRangeDate(range: TimeRange): Date {
  const now = new Date();
  switch (range) {
    case '1h':
      return new Date(now.getTime() - 60 * 60 * 1000);
    case '6h':
      return new Date(now.getTime() - 6 * 60 * 60 * 1000);
    case '24h':
      return new Date(now.getTime() - 24 * 60 * 60 * 1000);
    case '7d':
      return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    case '30d':
      return new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    case '90d':
      return new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
    case '1y':
      return new Date(now.getTime() - 365 * 24 * 60 * 60 * 1000);
  }
}

export function getDataSource(range: TimeRange): 'events' | 'hourly' | 'daily' {
  switch (range) {
    case '1h':
    case '6h':
    case '24h':
      return 'events';
    case '7d':
    case '30d':
      return 'hourly';
    case '90d':
    case '1y':
      return 'daily';
  }
}

export function TimeRangeSelector({ value, onChange }: TimeRangeSelectorProps) {
  return (
    <div className="flex gap-1">
      {ranges.map((r) => (
        <Button
          key={r.value}
          variant={value === r.value ? 'default' : 'ghost'}
          size="sm"
          className="h-7 px-2.5 text-xs"
          onClick={() => onChange(r.value)}
        >
          {r.label}
        </Button>
      ))}
    </div>
  );
}
