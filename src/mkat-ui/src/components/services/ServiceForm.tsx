import { useState } from 'react';
import { MonitorType, Severity, ThresholdStrategy } from '../../api/types';
import type { CreateServiceRequest, CreateMonitorRequest } from '../../api/types';
import { MonitorDescription } from '../monitors/MonitorDescription';

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

  const updateMonitor = (index: number, field: keyof CreateMonitorRequest, value: number | undefined) => {
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
      return next;
    });
    setMonitors(updated);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="block text-sm font-medium text-gray-700">Name</label>
        <input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          className="mt-1 block w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 px-3 py-2 border"
          required
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700">Description</label>
        <textarea
          value={description}
          onChange={e => setDescription(e.target.value)}
          className="mt-1 block w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 px-3 py-2 border"
          rows={3}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700">Severity</label>
        <select
          value={severity}
          onChange={e => setSeverity(Number(e.target.value) as Severity)}
          className="mt-1 block w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 px-3 py-2 border"
        >
          <option value={Severity.Low}>Low</option>
          <option value={Severity.Medium}>Medium</option>
          <option value={Severity.High}>High</option>
          <option value={Severity.Critical}>Critical</option>
        </select>
      </div>

      {showMonitors && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <label className="block text-sm font-medium text-gray-700">Monitors</label>
            <button
              type="button"
              onClick={addMonitor}
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              + Add Monitor
            </button>
          </div>

          <div className="space-y-4">
            {monitors.map((monitor, index) => (
              <div key={index} className="border rounded p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <select
                    value={monitor.type}
                    onChange={e => updateMonitor(index, 'type', Number(e.target.value))}
                    className="rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                  >
                    <option value={MonitorType.Webhook}>Webhook</option>
                    <option value={MonitorType.Heartbeat}>Heartbeat</option>
                    <option value={MonitorType.HealthCheck}>Health Check</option>
                    <option value={MonitorType.Metric}>Metric</option>
                  </select>
                  {monitors.length > 1 && (
                    <button
                      type="button"
                      onClick={() => removeMonitor(index)}
                      className="text-sm text-red-600 hover:text-red-800"
                    >
                      Remove
                    </button>
                  )}
                </div>

                <MonitorDescription type={monitor.type} variant="compact" />

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs text-gray-600">Interval (seconds)</label>
                    <input
                      type="number"
                      value={monitor.intervalSeconds}
                      onChange={e => updateMonitor(index, 'intervalSeconds', Number(e.target.value))}
                      className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                      min={10}
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600">Grace period (seconds)</label>
                    <input
                      type="number"
                      value={monitor.gracePeriodSeconds ?? 30}
                      onChange={e => updateMonitor(index, 'gracePeriodSeconds', Number(e.target.value))}
                      className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                      min={0}
                    />
                  </div>
                </div>

                {monitor.type === MonitorType.Metric && (
                  <div className="space-y-3 border-t pt-3 mt-3">
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="block text-xs text-gray-600">Min Value</label>
                        <input
                          type="number"
                          step="any"
                          value={monitor.minValue ?? ''}
                          onChange={e => updateMonitor(index, 'minValue', e.target.value ? Number(e.target.value) : undefined)}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                          placeholder="Optional"
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600">Max Value</label>
                        <input
                          type="number"
                          step="any"
                          value={monitor.maxValue ?? ''}
                          onChange={e => updateMonitor(index, 'maxValue', e.target.value ? Number(e.target.value) : undefined)}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                          placeholder="Optional"
                        />
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label className="block text-xs text-gray-600">Threshold Strategy</label>
                        <select
                          value={monitor.thresholdStrategy ?? ThresholdStrategy.Immediate}
                          onChange={e => updateMonitor(index, 'thresholdStrategy', Number(e.target.value))}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                        >
                          <option value={ThresholdStrategy.Immediate}>Immediate</option>
                          <option value={ThresholdStrategy.ConsecutiveCount}>Consecutive Count</option>
                          <option value={ThresholdStrategy.TimeDurationAverage}>Time Window Average</option>
                          <option value={ThresholdStrategy.SampleCountAverage}>Sample Count Average</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600">Retention (days)</label>
                        <input
                          type="number"
                          value={monitor.retentionDays ?? 7}
                          onChange={e => updateMonitor(index, 'retentionDays', Number(e.target.value))}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                          min={1}
                          max={365}
                        />
                      </div>
                    </div>
                    {(monitor.thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                      monitor.thresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                      <div>
                        <label className="block text-xs text-gray-600">Threshold Count</label>
                        <input
                          type="number"
                          value={monitor.thresholdCount ?? 3}
                          onChange={e => updateMonitor(index, 'thresholdCount', Number(e.target.value))}
                          className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                          min={2}
                        />
                      </div>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {isLoading ? 'Saving...' : submitLabel}
      </button>
    </form>
  );
}
