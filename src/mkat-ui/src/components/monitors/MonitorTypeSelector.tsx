import { MonitorType } from '../../api/types';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface Props {
  value: MonitorType;
  onChange: (value: MonitorType) => void;
  triggerClassName?: string;
}

export function MonitorTypeSelector({ value, onChange, triggerClassName }: Props) {
  return (
    <Select
      value={String(value)}
      onValueChange={v => onChange(Number(v) as MonitorType)}
    >
      <SelectTrigger className={triggerClassName} size="sm">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={String(MonitorType.Webhook)}>Webhook</SelectItem>
        <SelectItem value={String(MonitorType.Heartbeat)}>Heartbeat</SelectItem>
        <SelectItem value={String(MonitorType.HealthCheck)}>Health Check</SelectItem>
        <SelectItem value={String(MonitorType.Metric)}>Metric</SelectItem>
      </SelectContent>
    </Select>
  );
}
