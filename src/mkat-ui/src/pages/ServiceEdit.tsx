import { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { ServiceForm } from '../components/services/ServiceForm';
import { MonitorType, ThresholdStrategy } from '../api/types';
import type { CreateServiceRequest, UpdateServiceRequest, CreateMonitorRequest, UpdateMonitorRequest, Monitor } from '../api/types';

export function ServiceEdit() {
  const { serviceId } = useParams({ strict: false }) as { serviceId: string };
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: service, isLoading } = useQuery({
    queryKey: ['services', serviceId],
    queryFn: () => servicesApi.get(serviceId),
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateServiceRequest) => servicesApi.update(serviceId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => servicesApi.delete(serviceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      navigate({ to: '/services' });
    },
  });

  const addMonitorMutation = useMutation({
    mutationFn: (data: CreateMonitorRequest) => servicesApi.addMonitor(serviceId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
    },
  });

  const updateMonitorMutation = useMutation({
    mutationFn: ({ monitorId, data }: { monitorId: string; data: UpdateMonitorRequest }) =>
      servicesApi.updateMonitor(serviceId, monitorId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
    },
  });

  const deleteMonitorMutation = useMutation({
    mutationFn: (monitorId: string) => servicesApi.deleteMonitor(serviceId, monitorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
    },
  });

  if (isLoading || !service) return <div>Loading...</div>;

  const handleSubmit = (data: CreateServiceRequest) => {
    updateMutation.mutate({
      name: data.name,
      description: data.description,
      severity: data.severity,
    });
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Edit Service</h1>
      <div className="bg-white rounded-lg shadow p-6">
        <ServiceForm
          initialData={{
            name: service.name,
            description: service.description ?? undefined,
            severity: service.severity,
          }}
          onSubmit={handleSubmit}
          isLoading={updateMutation.isPending}
          submitLabel="Update Service"
          showMonitors={false}
        />
        {updateMutation.isError && (
          <p className="text-red-600 text-sm mt-4">
            {(updateMutation.error as Error).message}
          </p>
        )}
      </div>

      <MonitorSection
        monitors={service.monitors}
        onAdd={(data) => addMonitorMutation.mutate(data)}
        onUpdate={(monitorId, data) => updateMonitorMutation.mutate({ monitorId, data })}
        onDelete={(monitorId) => deleteMonitorMutation.mutate(monitorId)}
        isAdding={addMonitorMutation.isPending}
        addError={addMonitorMutation.isError ? (addMonitorMutation.error as Error).message : undefined}
        deleteError={deleteMonitorMutation.isError ? (deleteMonitorMutation.error as Error).message : undefined}
      />

      <div className="mt-6 bg-white rounded-lg shadow p-6 border border-red-200">
        <h2 className="text-lg font-semibold text-red-800 mb-2">Danger Zone</h2>
        <p className="text-sm text-gray-600 mb-4">
          Deleting a service removes all monitors and alert history.
        </p>
        <button
          onClick={() => {
            if (confirm('Are you sure you want to delete this service?')) {
              deleteMutation.mutate();
            }
          }}
          className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
        >
          Delete Service
        </button>
      </div>
    </div>
  );
}

function MonitorSection({
  monitors,
  onAdd,
  onUpdate,
  onDelete,
  isAdding,
  addError,
  deleteError,
}: {
  monitors: Monitor[];
  onAdd: (data: CreateMonitorRequest) => void;
  onUpdate: (monitorId: string, data: UpdateMonitorRequest) => void;
  onDelete: (monitorId: string) => void;
  isAdding: boolean;
  addError?: string;
  deleteError?: string;
}) {
  const [showAddForm, setShowAddForm] = useState(false);
  const [newType, setNewType] = useState<MonitorType>(MonitorType.Heartbeat);
  const [newInterval, setNewInterval] = useState(300);
  const [newGrace, setNewGrace] = useState(60);
  const [newMinValue, setNewMinValue] = useState<number | undefined>(undefined);
  const [newMaxValue, setNewMaxValue] = useState<number | undefined>(undefined);
  const [newThresholdStrategy, setNewThresholdStrategy] = useState<ThresholdStrategy>(ThresholdStrategy.Immediate);
  const [newThresholdCount, setNewThresholdCount] = useState(3);
  const [newRetentionDays, setNewRetentionDays] = useState(7);

  const handleAdd = () => {
    const data: CreateMonitorRequest = {
      type: newType,
      intervalSeconds: newInterval,
      gracePeriodSeconds: newGrace,
    };
    if (newType === MonitorType.Metric) {
      data.minValue = newMinValue;
      data.maxValue = newMaxValue;
      data.thresholdStrategy = newThresholdStrategy;
      if (newThresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
          newThresholdStrategy === ThresholdStrategy.SampleCountAverage) {
        data.thresholdCount = newThresholdCount;
      }
      data.retentionDays = newRetentionDays;
    }
    onAdd(data);
    setShowAddForm(false);
    setNewInterval(300);
    setNewGrace(60);
    setNewMinValue(undefined);
    setNewMaxValue(undefined);
    setNewThresholdStrategy(ThresholdStrategy.Immediate);
    setNewThresholdCount(3);
    setNewRetentionDays(7);
  };

  return (
    <div className="mt-6 bg-white rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Monitors</h2>
        <button
          type="button"
          onClick={() => setShowAddForm(!showAddForm)}
          className="text-sm text-blue-600 hover:text-blue-800"
        >
          {showAddForm ? 'Cancel' : '+ Add Monitor'}
        </button>
      </div>

      {showAddForm && (
        <div className="border rounded p-4 mb-4 bg-gray-50 space-y-3">
          <div>
            <label className="block text-xs text-gray-600">Type</label>
            <select
              value={newType}
              onChange={e => setNewType(Number(e.target.value) as MonitorType)}
              className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
            >
              <option value={MonitorType.Webhook}>Webhook</option>
              <option value={MonitorType.Heartbeat}>Heartbeat</option>
              <option value={MonitorType.HealthCheck}>Health Check</option>
              <option value={MonitorType.Metric}>Metric</option>
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-gray-600">Interval (seconds)</label>
              <input
                type="number"
                value={newInterval}
                onChange={e => setNewInterval(Number(e.target.value))}
                className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                min={30}
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600">Grace period (seconds)</label>
              <input
                type="number"
                value={newGrace}
                onChange={e => setNewGrace(Number(e.target.value))}
                className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                min={60}
              />
            </div>
          </div>
          {newType === MonitorType.Metric && (
            <div className="space-y-3 border-t pt-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600">Min Value</label>
                  <input
                    type="number"
                    step="any"
                    value={newMinValue ?? ''}
                    onChange={e => setNewMinValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    placeholder="Optional"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-600">Max Value</label>
                  <input
                    type="number"
                    step="any"
                    value={newMaxValue ?? ''}
                    onChange={e => setNewMaxValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    placeholder="Optional"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600">Threshold Strategy</label>
                  <select
                    value={newThresholdStrategy}
                    onChange={e => setNewThresholdStrategy(Number(e.target.value) as ThresholdStrategy)}
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
                    value={newRetentionDays}
                    onChange={e => setNewRetentionDays(Number(e.target.value))}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    min={1}
                    max={365}
                  />
                </div>
              </div>
              {(newThresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                newThresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                <div>
                  <label className="block text-xs text-gray-600">Threshold Count</label>
                  <input
                    type="number"
                    value={newThresholdCount}
                    onChange={e => setNewThresholdCount(Number(e.target.value))}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    min={2}
                  />
                </div>
              )}
            </div>
          )}
          <button
            type="button"
            onClick={handleAdd}
            disabled={isAdding}
            className="px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {isAdding ? 'Adding...' : 'Add'}
          </button>
          {addError && <p className="text-red-600 text-xs">{addError}</p>}
        </div>
      )}

      {deleteError && <p className="text-red-600 text-xs mb-2">{deleteError}</p>}

      <div className="space-y-3">
        {monitors.map(monitor => (
          <MonitorRow
            key={monitor.id}
            monitor={monitor}
            canDelete={monitors.length > 1}
            onUpdate={(data) => onUpdate(monitor.id, data)}
            onDelete={() => onDelete(monitor.id)}
          />
        ))}
      </div>
    </div>
  );
}

function MonitorRow({
  monitor,
  canDelete,
  onUpdate,
  onDelete,
}: {
  monitor: Monitor;
  canDelete: boolean;
  onUpdate: (data: UpdateMonitorRequest) => void;
  onDelete: () => void;
}) {
  const [editing, setEditing] = useState(false);
  const [interval, setInterval] = useState(monitor.intervalSeconds);
  const [grace, setGrace] = useState(monitor.gracePeriodSeconds);
  const [minValue, setMinValue] = useState<number | undefined>(monitor.minValue ?? undefined);
  const [maxValue, setMaxValue] = useState<number | undefined>(monitor.maxValue ?? undefined);
  const [thresholdStrategy, setThresholdStrategy] = useState<ThresholdStrategy>(monitor.thresholdStrategy ?? ThresholdStrategy.Immediate);
  const [thresholdCount, setThresholdCount] = useState(monitor.thresholdCount ?? 3);
  const [retentionDays, setRetentionDays] = useState(monitor.retentionDays ?? 7);

  const typeLabels: Record<MonitorType, string> = {
    [MonitorType.Webhook]: 'Webhook',
    [MonitorType.Heartbeat]: 'Heartbeat',
    [MonitorType.HealthCheck]: 'Health Check',
    [MonitorType.Metric]: 'Metric',
  };

  const handleSave = () => {
    const data: UpdateMonitorRequest = { intervalSeconds: interval, gracePeriodSeconds: grace };
    if (monitor.type === MonitorType.Metric) {
      data.minValue = minValue;
      data.maxValue = maxValue;
      data.thresholdStrategy = thresholdStrategy;
      if (thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
          thresholdStrategy === ThresholdStrategy.SampleCountAverage) {
        data.thresholdCount = thresholdCount;
      }
      data.retentionDays = retentionDays;
    }
    onUpdate(data);
    setEditing(false);
  };

  const handleCancel = () => {
    setInterval(monitor.intervalSeconds);
    setGrace(monitor.gracePeriodSeconds);
    setMinValue(monitor.minValue ?? undefined);
    setMaxValue(monitor.maxValue ?? undefined);
    setThresholdStrategy(monitor.thresholdStrategy ?? ThresholdStrategy.Immediate);
    setThresholdCount(monitor.thresholdCount ?? 3);
    setRetentionDays(monitor.retentionDays ?? 7);
    setEditing(false);
  };

  return (
    <div className="border rounded p-3">
      <div className="flex items-center justify-between mb-2">
        <span className="text-sm font-medium text-gray-700">{typeLabels[monitor.type]}</span>
        <div className="flex gap-2">
          {!editing && (
            <button
              type="button"
              onClick={() => setEditing(true)}
              className="text-xs text-blue-600 hover:text-blue-800"
            >
              Edit
            </button>
          )}
          <button
            type="button"
            onClick={() => {
              if (confirm('Remove this monitor?')) onDelete();
            }}
            disabled={!canDelete}
            className="text-xs text-red-600 hover:text-red-800 disabled:opacity-30 disabled:cursor-not-allowed"
          >
            Remove
          </button>
        </div>
      </div>

      {editing ? (
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-gray-600">Interval (seconds)</label>
              <input
                type="number"
                value={interval}
                onChange={e => setInterval(Number(e.target.value))}
                className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                min={30}
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600">Grace period (seconds)</label>
              <input
                type="number"
                value={grace}
                onChange={e => setGrace(Number(e.target.value))}
                className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                min={60}
              />
            </div>
          </div>
          {monitor.type === MonitorType.Metric && (
            <div className="space-y-2 border-t pt-2 mt-2">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600">Min Value</label>
                  <input
                    type="number"
                    step="any"
                    value={minValue ?? ''}
                    onChange={e => setMinValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    placeholder="Optional"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-600">Max Value</label>
                  <input
                    type="number"
                    step="any"
                    value={maxValue ?? ''}
                    onChange={e => setMaxValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    placeholder="Optional"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600">Threshold Strategy</label>
                  <select
                    value={thresholdStrategy}
                    onChange={e => setThresholdStrategy(Number(e.target.value) as ThresholdStrategy)}
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
                    value={retentionDays}
                    onChange={e => setRetentionDays(Number(e.target.value))}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    min={1}
                    max={365}
                  />
                </div>
              </div>
              {(thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                thresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                <div>
                  <label className="block text-xs text-gray-600">Threshold Count</label>
                  <input
                    type="number"
                    value={thresholdCount}
                    onChange={e => setThresholdCount(Number(e.target.value))}
                    className="mt-1 block w-full rounded border-gray-300 shadow-sm text-sm px-2 py-1 border"
                    min={2}
                  />
                </div>
              )}
            </div>
          )}
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleSave}
              className="px-2 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700"
            >
              Save
            </button>
            <button
              type="button"
              onClick={handleCancel}
              className="px-2 py-1 bg-gray-200 text-gray-700 text-xs rounded hover:bg-gray-300"
            >
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <div className="text-xs text-gray-500">
          Interval: {monitor.intervalSeconds}s | Grace: {monitor.gracePeriodSeconds}s
          {monitor.type === MonitorType.Metric && (
            <span>
              {monitor.minValue != null && ` | Min: ${monitor.minValue}`}
              {monitor.maxValue != null && ` | Max: ${monitor.maxValue}`}
              {monitor.thresholdStrategy != null && ` | ${ThresholdStrategy[monitor.thresholdStrategy]}`}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
