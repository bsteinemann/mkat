import { useState } from 'react';
import { MonitorType, Severity } from '../../api/types';
import type { CreateServiceRequest, CreateMonitorRequest } from '../../api/types';
import { MonitorDescription } from '../monitors/MonitorDescription';
import { MonitorTypeSelector } from '../monitors/MonitorTypeSelector';
import { IntervalGraceFields } from '../monitors/IntervalGraceFields';
import { HealthCheckFields } from '../monitors/HealthCheckFields';
import { MetricFields } from '../monitors/MetricFields';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface Props {
  initialData?: {
    name: string;
    description?: string;
    severity: Severity;
  };
  onSubmit: (data: CreateServiceRequest) => void;
  isLoading?: boolean;
  submitLabel?: string;
  showMonitors?: boolean;
}

export function ServiceForm({
  initialData,
  onSubmit,
  isLoading,
  submitLabel = 'Create',
  showMonitors = true,
}: Props) {
  const [name, setName] = useState(initialData?.name ?? '');
  const [description, setDescription] = useState(initialData?.description ?? '');
  const [severity, setSeverity] = useState<Severity>(initialData?.severity ?? Severity.Medium);
  const [monitors, setMonitors] = useState<CreateMonitorRequest[]>([
    { type: MonitorType.Heartbeat, intervalSeconds: 60, gracePeriodSeconds: 60 },
  ]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      name,
      description: description || undefined,
      severity,
      monitors: showMonitors ? monitors : [],
    });
  };

  const addMonitor = () => {
    setMonitors([
      ...monitors,
      { type: MonitorType.Heartbeat, intervalSeconds: 60, gracePeriodSeconds: 30 },
    ]);
  };

  const removeMonitor = (index: number) => {
    setMonitors(monitors.filter((_, i) => i !== index));
  };

  const updateMonitor = (
    index: number,
    field: keyof CreateMonitorRequest,
    value: string | number | undefined,
  ) => {
    const updated = monitors.map((m, i) => {
      if (i !== index) return m;
      const next = { ...m, [field]: value };
      // Reset metric fields when switching away from Metric type
      if (field === 'type' && value !== MonitorType.Metric) {
        delete next.minValue;
        delete next.maxValue;
        delete next.thresholdStrategy;
        delete next.thresholdCount;
        delete next.retentionDays;
      }
      // Reset health check fields when switching away from HealthCheck type
      if (field === 'type' && value !== MonitorType.HealthCheck) {
        delete next.healthCheckUrl;
        delete next.httpMethod;
        delete next.expectedStatusCodes;
        delete next.timeoutSeconds;
        delete next.bodyMatchRegex;
      }
      return next;
    });
    setMonitors(updated);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="space-y-2">
        <Label>Name</Label>
        <Input type="text" value={name} onChange={(e) => setName(e.target.value)} required />
      </div>

      <div className="space-y-2">
        <Label>Description</Label>
        <Textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
      </div>

      <div className="space-y-2">
        <Label>Severity</Label>
        <Select value={String(severity)} onValueChange={(v) => setSeverity(Number(v) as Severity)}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={String(Severity.Low)}>Low</SelectItem>
            <SelectItem value={String(Severity.Medium)}>Medium</SelectItem>
            <SelectItem value={String(Severity.High)}>High</SelectItem>
            <SelectItem value={String(Severity.Critical)}>Critical</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {showMonitors && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <Label>Monitors</Label>
            <Button
              type="button"
              variant="link"
              size="sm"
              className="p-0 h-auto"
              onClick={addMonitor}
            >
              + Add Monitor
            </Button>
          </div>

          <div className="space-y-4">
            {monitors.map((monitor, index) => (
              <div key={index} className="border rounded p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <MonitorTypeSelector
                    value={monitor.type}
                    onChange={(v) => updateMonitor(index, 'type', v)}
                  />
                  {monitors.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="p-0 h-auto text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 hover:bg-transparent"
                      onClick={() => removeMonitor(index)}
                    >
                      Remove
                    </Button>
                  )}
                </div>

                <MonitorDescription type={monitor.type} variant="compact" />

                <IntervalGraceFields
                  intervalSeconds={monitor.intervalSeconds}
                  gracePeriodSeconds={monitor.gracePeriodSeconds ?? 60}
                  onIntervalChange={(v) => updateMonitor(index, 'intervalSeconds', v)}
                  onGracePeriodChange={(v) => updateMonitor(index, 'gracePeriodSeconds', v)}
                  minInterval={10}
                />

                {monitor.type === MonitorType.Metric && (
                  <MetricFields
                    values={{
                      minValue: monitor.minValue ?? undefined,
                      maxValue: monitor.maxValue ?? undefined,
                      thresholdStrategy: monitor.thresholdStrategy ?? 0,
                      thresholdCount: monitor.thresholdCount ?? 3,
                      retentionDays: monitor.retentionDays ?? 7,
                    }}
                    onChange={(field, value) => updateMonitor(index, field, value)}
                  />
                )}

                {monitor.type === MonitorType.HealthCheck && (
                  <HealthCheckFields
                    values={{
                      healthCheckUrl: monitor.healthCheckUrl ?? '',
                      httpMethod: monitor.httpMethod ?? 'GET',
                      expectedStatusCodes: monitor.expectedStatusCodes ?? '200',
                      timeoutSeconds: monitor.timeoutSeconds ?? 10,
                      bodyMatchRegex: monitor.bodyMatchRegex ?? '',
                    }}
                    onChange={(field, value) => updateMonitor(index, field, value)}
                    urlRequired
                  />
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <Button type="submit" disabled={isLoading} className="w-full">
        {isLoading ? 'Saving...' : submitLabel}
      </Button>
    </form>
  );
}
