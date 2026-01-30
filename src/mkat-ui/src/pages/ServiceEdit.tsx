import { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi, contactsApi } from '../api/services';
import { ServiceForm } from '../components/services/ServiceForm';
import { MonitorType, ThresholdStrategy } from '../api/types';
import type { CreateServiceRequest, UpdateServiceRequest, CreateMonitorRequest, UpdateMonitorRequest, Monitor } from '../api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

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

      <ContactsSection serviceId={serviceId} />

      <div className="mt-6 bg-white rounded-lg shadow p-6 border border-red-200">
        <h2 className="text-lg font-semibold text-red-800 mb-2">Danger Zone</h2>
        <p className="text-sm text-gray-600 mb-4">
          Deleting a service removes all monitors and alert history.
        </p>
        <Button
          variant="destructive"
          onClick={() => {
            if (confirm('Are you sure you want to delete this service?')) {
              deleteMutation.mutate();
            }
          }}
        >
          Delete Service
        </Button>
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
  const [newHealthCheckUrl, setNewHealthCheckUrl] = useState('');
  const [newHttpMethod, setNewHttpMethod] = useState('GET');
  const [newExpectedStatusCodes, setNewExpectedStatusCodes] = useState('200');
  const [newTimeoutSeconds, setNewTimeoutSeconds] = useState(10);
  const [newBodyMatchRegex, setNewBodyMatchRegex] = useState('');

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
    if (newType === MonitorType.HealthCheck) {
      data.healthCheckUrl = newHealthCheckUrl;
      data.httpMethod = newHttpMethod;
      data.expectedStatusCodes = newExpectedStatusCodes;
      data.timeoutSeconds = newTimeoutSeconds;
      data.bodyMatchRegex = newBodyMatchRegex || undefined;
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
    setNewHealthCheckUrl('');
    setNewHttpMethod('GET');
    setNewExpectedStatusCodes('200');
    setNewTimeoutSeconds(10);
    setNewBodyMatchRegex('');
  };

  return (
    <div className="mt-6 bg-white rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Monitors</h2>
        <Button
          type="button"
          variant="link"
          size="sm"
          className="p-0 h-auto"
          onClick={() => setShowAddForm(!showAddForm)}
        >
          {showAddForm ? 'Cancel' : '+ Add Monitor'}
        </Button>
      </div>

      {showAddForm && (
        <div className="border rounded p-4 mb-4 bg-gray-50 space-y-3">
          <div className="space-y-1">
            <Label className="text-xs text-muted-foreground">Type</Label>
            <Select value={String(newType)} onValueChange={v => setNewType(Number(v) as MonitorType)}>
              <SelectTrigger className="w-full h-8 text-sm" size="sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(MonitorType.Webhook)}>Webhook</SelectItem>
                <SelectItem value={String(MonitorType.Heartbeat)}>Heartbeat</SelectItem>
                <SelectItem value={String(MonitorType.HealthCheck)}>Health Check</SelectItem>
                <SelectItem value={String(MonitorType.Metric)}>Metric</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">Interval (seconds)</Label>
              <Input
                type="number"
                value={newInterval}
                onChange={e => setNewInterval(Number(e.target.value))}
                className="h-8 text-sm"
                min={30}
              />
            </div>
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">Grace period (seconds)</Label>
              <Input
                type="number"
                value={newGrace}
                onChange={e => setNewGrace(Number(e.target.value))}
                className="h-8 text-sm"
                min={60}
              />
            </div>
          </div>
          {newType === MonitorType.Metric && (
            <div className="space-y-3 border-t pt-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Min Value</Label>
                  <Input
                    type="number"
                    step="any"
                    value={newMinValue ?? ''}
                    onChange={e => setNewMinValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="h-8 text-sm"
                    placeholder="Optional"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Max Value</Label>
                  <Input
                    type="number"
                    step="any"
                    value={newMaxValue ?? ''}
                    onChange={e => setNewMaxValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="h-8 text-sm"
                    placeholder="Optional"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Threshold Strategy</Label>
                  <Select value={String(newThresholdStrategy)} onValueChange={v => setNewThresholdStrategy(Number(v) as ThresholdStrategy)}>
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
                    value={newRetentionDays}
                    onChange={e => setNewRetentionDays(Number(e.target.value))}
                    className="h-8 text-sm"
                    min={1}
                    max={365}
                  />
                </div>
              </div>
              {(newThresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                newThresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Threshold Count</Label>
                  <Input
                    type="number"
                    value={newThresholdCount}
                    onChange={e => setNewThresholdCount(Number(e.target.value))}
                    className="h-8 text-sm"
                    min={2}
                  />
                </div>
              )}
            </div>
          )}
          {newType === MonitorType.HealthCheck && (
            <div className="space-y-3 border-t pt-3">
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">URL</Label>
                <Input
                  type="url"
                  value={newHealthCheckUrl}
                  onChange={e => setNewHealthCheckUrl(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="https://example.com/health"
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">HTTP Method</Label>
                  <Select value={newHttpMethod} onValueChange={v => setNewHttpMethod(v)}>
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
                    value={newTimeoutSeconds}
                    onChange={e => setNewTimeoutSeconds(Number(e.target.value))}
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
                  value={newExpectedStatusCodes}
                  onChange={e => setNewExpectedStatusCodes(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="200,201,204"
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Body Match Regex (optional)</Label>
                <Input
                  type="text"
                  value={newBodyMatchRegex}
                  onChange={e => setNewBodyMatchRegex(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="ok|healthy"
                />
              </div>
            </div>
          )}
          <Button
            type="button"
            size="sm"
            onClick={handleAdd}
            disabled={isAdding}
          >
            {isAdding ? 'Adding...' : 'Add'}
          </Button>
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

function ContactsSection({ serviceId }: { serviceId: string }) {
  const queryClient = useQueryClient();

  const { data: allContacts, isLoading: loadingContacts } = useQuery({
    queryKey: ['contacts'],
    queryFn: () => contactsApi.list(),
  });

  const { data: assignedContacts, isLoading: loadingAssigned } = useQuery({
    queryKey: ['services', serviceId, 'contacts'],
    queryFn: () => contactsApi.getServiceContacts(serviceId),
  });

  const [localSelected, setLocalSelected] = useState<Set<string> | null>(null);

  const serverSet = new Set(assignedContacts?.map(c => c.id) ?? []);
  const selected = localSelected ?? serverSet;
  const dirty = localSelected !== null;

  const saveMutation = useMutation({
    mutationFn: (contactIds: string[]) => contactsApi.setServiceContacts(serviceId, contactIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId, 'contacts'] });
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      setLocalSelected(null);
    },
  });

  const toggle = (id: string) => {
    const current = localSelected ?? serverSet;
    const next = new Set(current);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    setLocalSelected(next);
  };

  if (loadingContacts || loadingAssigned) {
    return (
      <div className="mt-6 bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Contacts</h2>
        <p className="text-sm text-gray-500">Loading...</p>
      </div>
    );
  }

  return (
    <div className="mt-6 bg-white rounded-lg shadow p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-2">Contacts</h2>
      <p className="text-xs text-gray-500 mb-4">
        Select which contacts receive alerts for this service. If none are assigned, the default contact is used.
      </p>

      {!allContacts || allContacts.length === 0 ? (
        <p className="text-sm text-gray-500">No contacts configured yet.</p>
      ) : (
        <div className="space-y-2">
          {allContacts.map(contact => (
            <label key={contact.id} className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={selected.has(contact.id)}
                onChange={() => toggle(contact.id)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700">{contact.name}</span>
              {contact.isDefault && (
                <span className="text-xs text-gray-400">(default)</span>
              )}
              {contact.channels.length === 0 && (
                <span className="text-xs text-amber-600">(no channels)</span>
              )}
            </label>
          ))}
        </div>
      )}

      {dirty && (
        <div className="mt-4 flex items-center gap-2">
          <Button
            size="sm"
            onClick={() => saveMutation.mutate(Array.from(selected))}
            disabled={saveMutation.isPending}
          >
            {saveMutation.isPending ? 'Saving...' : 'Save Contacts'}
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => setLocalSelected(null)}
          >
            Cancel
          </Button>
        </div>
      )}

      {saveMutation.isError && (
        <p className="text-red-600 text-xs mt-2">
          {(saveMutation.error as Error).message}
        </p>
      )}
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
  const [healthCheckUrl, setHealthCheckUrl] = useState(monitor.healthCheckUrl ?? '');
  const [httpMethod, setHttpMethod] = useState(monitor.httpMethod ?? 'GET');
  const [expectedStatusCodes, setExpectedStatusCodes] = useState(monitor.expectedStatusCodes ?? '200');
  const [timeoutSeconds, setTimeoutSeconds] = useState(monitor.timeoutSeconds ?? 10);
  const [bodyMatchRegex, setBodyMatchRegex] = useState(monitor.bodyMatchRegex ?? '');

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
    if (monitor.type === MonitorType.HealthCheck) {
      data.healthCheckUrl = healthCheckUrl;
      data.httpMethod = httpMethod;
      data.expectedStatusCodes = expectedStatusCodes;
      data.timeoutSeconds = timeoutSeconds;
      data.bodyMatchRegex = bodyMatchRegex || undefined;
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
    setHealthCheckUrl(monitor.healthCheckUrl ?? '');
    setHttpMethod(monitor.httpMethod ?? 'GET');
    setExpectedStatusCodes(monitor.expectedStatusCodes ?? '200');
    setTimeoutSeconds(monitor.timeoutSeconds ?? 10);
    setBodyMatchRegex(monitor.bodyMatchRegex ?? '');
    setEditing(false);
  };

  return (
    <div className="border rounded p-3">
      <div className="flex items-center justify-between mb-2">
        <span className="text-sm font-medium text-gray-700">{typeLabels[monitor.type]}</span>
        <div className="flex gap-2">
          {!editing && (
            <Button
              type="button"
              variant="link"
              size="xs"
              className="p-0 h-auto"
              onClick={() => setEditing(true)}
            >
              Edit
            </Button>
          )}
          <Button
            type="button"
            variant="ghost"
            size="xs"
            className="p-0 h-auto text-red-600 hover:text-red-800 hover:bg-transparent"
            onClick={() => {
              if (confirm('Remove this monitor?')) onDelete();
            }}
            disabled={!canDelete}
          >
            Remove
          </Button>
        </div>
      </div>

      {editing ? (
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">Interval (seconds)</Label>
              <Input
                type="number"
                value={interval}
                onChange={e => setInterval(Number(e.target.value))}
                className="h-8 text-sm"
                min={30}
              />
            </div>
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">Grace period (seconds)</Label>
              <Input
                type="number"
                value={grace}
                onChange={e => setGrace(Number(e.target.value))}
                className="h-8 text-sm"
                min={60}
              />
            </div>
          </div>
          {monitor.type === MonitorType.Metric && (
            <div className="space-y-2 border-t pt-2 mt-2">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Min Value</Label>
                  <Input
                    type="number"
                    step="any"
                    value={minValue ?? ''}
                    onChange={e => setMinValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="h-8 text-sm"
                    placeholder="Optional"
                  />
                </div>
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Max Value</Label>
                  <Input
                    type="number"
                    step="any"
                    value={maxValue ?? ''}
                    onChange={e => setMaxValue(e.target.value ? Number(e.target.value) : undefined)}
                    className="h-8 text-sm"
                    placeholder="Optional"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Threshold Strategy</Label>
                  <Select value={String(thresholdStrategy)} onValueChange={v => setThresholdStrategy(Number(v) as ThresholdStrategy)}>
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
                    value={retentionDays}
                    onChange={e => setRetentionDays(Number(e.target.value))}
                    className="h-8 text-sm"
                    min={1}
                    max={365}
                  />
                </div>
              </div>
              {(thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
                thresholdStrategy === ThresholdStrategy.SampleCountAverage) && (
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">Threshold Count</Label>
                  <Input
                    type="number"
                    value={thresholdCount}
                    onChange={e => setThresholdCount(Number(e.target.value))}
                    className="h-8 text-sm"
                    min={2}
                  />
                </div>
              )}
            </div>
          )}
          {monitor.type === MonitorType.HealthCheck && (
            <div className="space-y-2 border-t pt-2 mt-2">
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">URL</Label>
                <Input
                  type="url"
                  value={healthCheckUrl}
                  onChange={e => setHealthCheckUrl(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="https://example.com/health"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label className="text-xs text-muted-foreground">HTTP Method</Label>
                  <Select value={httpMethod} onValueChange={v => setHttpMethod(v)}>
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
                    value={timeoutSeconds}
                    onChange={e => setTimeoutSeconds(Number(e.target.value))}
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
                  value={expectedStatusCodes}
                  onChange={e => setExpectedStatusCodes(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="200,201,204"
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Body Match Regex (optional)</Label>
                <Input
                  type="text"
                  value={bodyMatchRegex}
                  onChange={e => setBodyMatchRegex(e.target.value)}
                  className="h-8 text-sm"
                  placeholder="ok|healthy"
                />
              </div>
            </div>
          )}
          <div className="flex gap-2">
            <Button
              type="button"
              size="xs"
              onClick={handleSave}
            >
              Save
            </Button>
            <Button
              type="button"
              variant="secondary"
              size="xs"
              onClick={handleCancel}
            >
              Cancel
            </Button>
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
          {monitor.type === MonitorType.HealthCheck && (
            <span>
              {` | URL: ${monitor.healthCheckUrl}`}
              {` | ${monitor.httpMethod ?? 'GET'}`}
              {` | ${monitor.expectedStatusCodes ?? '200'}`}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
