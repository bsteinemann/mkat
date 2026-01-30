import { Button } from '@/components/ui/button';
import type { TimeRange } from './timeRangeUtils';

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
