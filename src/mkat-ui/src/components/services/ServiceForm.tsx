import { useState } from 'react';
import { MonitorType, Severity } from '../../api/types';
import type { CreateServiceRequest, CreateMonitorRequest } from '../../api/types';

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

  const updateMonitor = (index: number, field: keyof CreateMonitorRequest, value: number) => {
    setMonitors(monitors.map((m, i) => i === index ? { ...m, [field]: value } : m));
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
