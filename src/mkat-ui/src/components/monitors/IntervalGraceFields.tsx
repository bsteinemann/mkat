import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Props {
  intervalSeconds: number;
  gracePeriodSeconds: number;
  onIntervalChange: (value: number) => void;
  onGracePeriodChange: (value: number) => void;
  minInterval?: number;
}

export function IntervalGraceFields({
  intervalSeconds,
  gracePeriodSeconds,
  onIntervalChange,
  onGracePeriodChange,
  minInterval = 30,
}: Props) {
  return (
    <div className="grid grid-cols-2 gap-3">
      <div className="space-y-1">
        <Label className="text-xs text-muted-foreground">Interval (seconds)</Label>
        <Input
          type="number"
          value={intervalSeconds}
          onChange={(e) => onIntervalChange(Number(e.target.value))}
          className="h-8 text-sm"
          min={minInterval}
        />
      </div>
      <div className="space-y-1">
        <Label className="text-xs text-muted-foreground">Grace period (seconds)</Label>
        <Input
          type="number"
          value={gracePeriodSeconds}
          onChange={(e) => onGracePeriodChange(Number(e.target.value))}
          className="h-8 text-sm"
          min={0}
        />
      </div>
    </div>
  );
}
