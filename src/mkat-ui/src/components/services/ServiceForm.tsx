import { useState } from 'react';
import { MonitorType, Severity, ThresholdStrategy } from '../../api/types';
import type { CreateServiceRequest, CreateMonitorRequest } from '../../api/types';
import { MonitorDescription } from '../monitors/MonitorDescription';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

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

export function ServiceForm({ initialData, onSubmit, isLoading, submitLabel = 'Create', showMonitors = true }: Props) {
  const [name, setName] = useState(initialData?.name ?? '');
  const [description, setDescription] = useState(initialData?.description ?? '');
  const [severity, setSeverity] = useState<Severity>(initialData?.severity ?? Severity.Medium);
  const [monitors, setMonitors] = useState<CreateMonitorRequest[]>([
    { type: MonitorType.Heartbeat, intervalSeconds: 60, gracePeriodSeconds: 30 }
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
    setMonitors([...monitors, { type: MonitorType.Heartbeat, intervalSeconds: 60, gracePeriodSeconds: 30 }]);
  };

  const removeMonitor = (index: number) => {
    setMonitors(monitors.filter((_, i) => i !== index));
  };

  const updateMonitor = (index: number, field: keyof CreateMonitorRequest, value: string | number | undefined) => {
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
        <Input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          required
        />
      </div>

      <div className="space-y-2">
        <Label>Description</Label>
        <Textarea
          value={description}
          onChange={e => setDescription(e.target.value)}
          rows={3}
        />
      </div>

      <div className="space-y-2">
        <Label>Severity</Label>
        <Select value={String(severity)} onValueChange={v => setSeverity(Number(v) as Severity)}>
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
                  <Select
                    value={String(monitor.type)}
                    onValueChange={v => updateMonitor(index, 'type', Number(v))}
                  >
                    <SelectTrigger size="sm">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={String(MonitorType.Webhook)}>Webhook</SelectItem>
                      <SelectItem value={String(MonitorType.Heartbeat)}>Heartbeat</SelectItem>
                      <SelectItem value={String(MonitorType.HealthCheck)}>Health Check</SelectItem>
                      <SelectItem value={String(MonitorType.Metric)}>Metric</SelectItem>
                    </SelectContent>
                  </Select>
                  {monitors.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="p-0 h-auto text-red-600 hover:text-red-800 hover:bg-transparent"
                      onClick={() => removeMonitor(index)}
                    >
                      Remove
                    </Button>
                  )}
                </div>

                <MonitorDescription type={monitor.type} variant="compact" />

                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <Label className="text-xs text-muted-foreground">Interval (seconds)</Label>
                    <Input
                      type="number"
                      value={monitor.intervalSeconds}
                      onChange={e => updateMonitor(index, 'intervalSeconds', Number(e.target.value))}
                      className="h-8 text-sm"
                      min={10}
                    />
                  </div>
                  <div className="space-y-1">
                    <Label className="text-xs text-muted-foreground">Grace period (seconds)</Label>
                    <Input
                      type="number"
                      value={monitor.gracePeriodSeconds ?? 30}
                      onChange={e => updateMonitor(index, 'gracePeriodSeconds', Number(e.target.value))}
                      className="h-8 text-sm"
                      min={0}
                    />
                  </div>
                </div>

                {monitor.type === MonitorType.Metric && (
                  <div className="space-y-3 border-t pt-3 mt-3">
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">Min Value</Label>
                        <Input
                          type="number"
                          step="any"
                          value={monitor.minValue ?? ''}
                          onChange={e => updateMonitor(index, 'minValue', e.target.value ? Number(e.target.value) : undefined)}
                          className="h-8 text-sm"
                          placeholder="Optional"
                        />
                      </div>
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">Max Value</Label>
                        <Input
                          type="number"
                          step="any"
                          value={monitor.maxValue ?? ''}
                          onChange={e => updateMonitor(index, 'maxValue', e.target.value ? Number(e.target.value) : undefined)}
                          className="h-8 text-sm"
                          placeholder="Optional"
                        />
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">Threshold Strategy</Label>
                        <Select
                          value={String(monitor.thresholdStrategy ?? ThresholdStrategy.Immediate)}
                          onValueChange={v => updateMonitor(index, 'thresholdStrategy', Number(v))}
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
                          value={monitor.retentionDays ?? 7}
                          onChange={e => updateMonitor(index, 'retentionDays', Number(e.target.value))}
                          className="h-8 text-sm"
                          min={1}
                          max={365}
                        />
                      </div>
                    </div>
                    {(monitor.thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                      monitor.thresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">Threshold Count</Label>
                        <Input
                          type="number"
                          value={monitor.thresholdCount ?? 3}
                          onChange={e => updateMonitor(index, 'thresholdCount', Number(e.target.value))}
                          className="h-8 text-sm"
                          min={2}
                        />
                      </div>
                    )}
                  </div>
                )}

                {monitor.type === MonitorType.HealthCheck && (
                  <div className="space-y-3 border-t pt-3 mt-3">
                    <div className="space-y-1">
                      <Label className="text-xs text-muted-foreground">URL</Label>
                      <Input
                        type="url"
                        value={monitor.healthCheckUrl ?? ''}
                        onChange={e => updateMonitor(index, 'healthCheckUrl', e.target.value || undefined)}
                        className="h-8 text-sm"
                        placeholder="https://example.com/health"
                        required
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">HTTP Method</Label>
                        <Select
                          value={monitor.httpMethod ?? 'GET'}
                          onValueChange={v => updateMonitor(index, 'httpMethod', v)}
                        >
                          <SelectTrigger className="w-full h-8 text-sm" size="sm">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="GET">GET</SelectItem>
                            <SelectItem value="HEAD">HEAD</SelectItem>
                            <SelectItem value="POST">POST</SelectItem>
                            <SelectItem value="PUT">PUT</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-1">
                        <Label className="text-xs text-muted-foreground">Timeout (seconds)</Label>
                        <Input
                          type="number"
                          value={monitor.timeoutSeconds ?? 10}
                          onChange={e => updateMonitor(index, 'timeoutSeconds', Number(e.target.value))}
                          className="h-8 text-sm"
                          min={1}
                          max={120}
                        />
                      </div>
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs text-muted-foreground">Expected Status Codes</Label>
                      <Input
                        type="text"
                        value={monitor.expectedStatusCodes ?? '200'}
                        onChange={e => updateMonitor(index, 'expectedStatusCodes', e.target.value)}
                        className="h-8 text-sm"
                        placeholder="200,201,204"
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs text-muted-foreground">Body Match Regex (optional)</Label>
                      <Input
                        type="text"
                        value={monitor.bodyMatchRegex ?? ''}
                        onChange={e => updateMonitor(index, 'bodyMatchRegex', e.target.value || undefined)}
                        className="h-8 text-sm"
                        placeholder="ok|healthy"
                      />
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <Button
        type="submit"
        disabled={isLoading}
        className="w-full"
      >
        {isLoading ? 'Saving...' : submitLabel}
      </Button>
    </form>
  );
}
