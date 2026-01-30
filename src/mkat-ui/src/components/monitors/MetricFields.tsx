import { ThresholdStrategy } from '../../api/types';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

export interface MetricValues {
  minValue: number | undefined;
  maxValue: number | undefined;
  thresholdStrategy: ThresholdStrategy;
  thresholdCount: number;
  retentionDays: number;
}

interface Props {
  values: MetricValues;
  onChange: (field: keyof MetricValues, value: number | undefined) => void;
}

export function MetricFields({ values, onChange }: Props) {
  return (
    <div className="space-y-3 border-t pt-3 mt-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Min Value</Label>
          <Input
            type="number"
            step="any"
            value={values.minValue ?? ''}
            onChange={e => onChange('minValue', e.target.value ? Number(e.target.value) : undefined)}
            className="h-8 text-sm"
            placeholder="Optional"
          />
        </div>
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Max Value</Label>
          <Input
            type="number"
            step="any"
            value={values.maxValue ?? ''}
            onChange={e => onChange('maxValue', e.target.value ? Number(e.target.value) : undefined)}
            className="h-8 text-sm"
            placeholder="Optional"
          />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Threshold Strategy</Label>
          <Select
            value={String(values.thresholdStrategy)}
            onValueChange={v => onChange('thresholdStrategy', Number(v))}
          >
            <SelectTrigger className="w-full h-8 text-sm" size="sm">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={String(ThresholdStrategy.Immediate)}>Immediate</SelectItem>
              <SelectItem value={String(ThresholdStrategy.ConsecutiveCount)}>Consecutive Count</SelectItem>
              <SelectItem value={String(ThresholdStrategy.TimeDurationAverage)}>Time Window Average</SelectItem>
              <SelectItem value={String(ThresholdStrategy.SampleCountAverage)}>Sample Count Average</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Retention (days)</Label>
          <Input
            type="number"
            value={values.retentionDays}
            onChange={e => onChange('retentionDays', Number(e.target.value))}
            className="h-8 text-sm"
            min={1}
            max={365}
          />
        </div>
      </div>
      {(values.thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
        values.thresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Threshold Count</Label>
          <Input
            type="number"
            value={values.thresholdCount}
            onChange={e => onChange('thresholdCount', Number(e.target.value))}
            className="h-8 text-sm"
            min={2}
          />
        </div>
      )}
    </div>
  );
}
