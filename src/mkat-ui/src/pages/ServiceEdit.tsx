import { useState } from 'react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { getErrorMessage } from '../api/client';
import { servicesApi, contactsApi } from '../api/services';
import { ServiceForm } from '../components/services/ServiceForm';
import { MonitorTypeSelector } from '../components/monitors/MonitorTypeSelector';
import { IntervalGraceFields } from '../components/monitors/IntervalGraceFields';
import { HealthCheckFields } from '../components/monitors/HealthCheckFields';
import { MetricFields } from '../components/monitors/MetricFields';
import { MonitorType, ThresholdStrategy } from '../api/types';
import type {
  CreateServiceRequest,
  UpdateServiceRequest,
  CreateMonitorRequest,
  UpdateMonitorRequest,
  Monitor,
} from '../api/types';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';

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
      toast.success('Service updated');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update service'));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => servicesApi.delete(serviceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Service deleted');
      navigate({ to: '/services' });
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to delete service'));
    },
  });

  const addMonitorMutation = useMutation({
    mutationFn: (data: CreateMonitorRequest) => servicesApi.addMonitor(serviceId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
      toast.success('Monitor added');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to add monitor'));
    },
  });

  const updateMonitorMutation = useMutation({
    mutationFn: ({ monitorId, data }: { monitorId: string; data: UpdateMonitorRequest }) =>
      servicesApi.updateMonitor(serviceId, monitorId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
      toast.success('Monitor updated');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update monitor'));
    },
  });

  const deleteMonitorMutation = useMutation({
    mutationFn: (monitorId: string) => servicesApi.deleteMonitor(serviceId, monitorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId] });
      toast.success('Monitor removed');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to remove monitor'));
    },
  });

  if (isLoading || !service)
    return (
      <div className="max-w-2xl space-y-6">
        <Skeleton className="h-8 w-40" />
        <Skeleton className="h-52 w-full rounded-lg" />
        <Skeleton className="h-40 w-full rounded-lg" />
      </div>
    );

  const handleSubmit = (data: CreateServiceRequest) => {
    updateMutation.mutate({
      name: data.name,
      description: data.description,
      severity: data.severity,
    });
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-foreground mb-6">Edit Service</h1>
      <Card className="py-0">
        <CardContent className="p-6">
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
        </CardContent>
      </Card>

      <MonitorSection
        monitors={service.monitors}
        onAdd={(data) => addMonitorMutation.mutate(data)}
        onUpdate={(monitorId, data) => updateMonitorMutation.mutate({ monitorId, data })}
        onDelete={(monitorId) => deleteMonitorMutation.mutate(monitorId)}
        isAdding={addMonitorMutation.isPending}
      />

      <ContactsSection serviceId={serviceId} />

      <Card className="mt-6 border-red-200 dark:border-red-800 py-0">
        <CardHeader>
          <CardTitle className="text-lg text-red-800 dark:text-red-200">Danger Zone</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground mb-4">
            Deleting a service removes all monitors and alert history.
          </p>
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive">Delete Service</Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete service "{service.name}"?</AlertDialogTitle>
                <AlertDialogDescription>
                  This will permanently delete the service and all its monitors and alerts.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction onClick={() => deleteMutation.mutate()}>
                  Delete
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </CardContent>
      </Card>
    </div>
  );
}

function MonitorSection({
  monitors,
  onAdd,
  onUpdate,
  onDelete,
  isAdding,
}: {
  monitors: Monitor[];
  onAdd: (data: CreateMonitorRequest) => void;
  onUpdate: (monitorId: string, data: UpdateMonitorRequest) => void;
  onDelete: (monitorId: string) => void;
  isAdding: boolean;
}) {
  const [showAddForm, setShowAddForm] = useState(false);
  const [newType, setNewType] = useState<MonitorType>(MonitorType.Heartbeat);
  const [newInterval, setNewInterval] = useState(300);
  const [newGrace, setNewGrace] = useState(60);
  const [newMinValue, setNewMinValue] = useState<number | undefined>(undefined);
  const [newMaxValue, setNewMaxValue] = useState<number | undefined>(undefined);
  const [newThresholdStrategy, setNewThresholdStrategy] = useState<ThresholdStrategy>(
    ThresholdStrategy.Immediate,
  );
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
      if (
        newThresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
        newThresholdStrategy === ThresholdStrategy.SampleCountAverage
      ) {
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
    <Card className="mt-6 py-0">
      <CardContent className="p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">Monitors</h2>
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
          <div className="border rounded p-4 mb-4 bg-muted space-y-3">
            <div className="space-y-1">
              <Label className="text-xs text-muted-foreground">Type</Label>
              <MonitorTypeSelector
                value={newType}
                onChange={setNewType}
                triggerClassName="w-full h-8 text-sm"
              />
            </div>
            <IntervalGraceFields
              intervalSeconds={newInterval}
              gracePeriodSeconds={newGrace}
              onIntervalChange={setNewInterval}
              onGracePeriodChange={setNewGrace}
            />
            {newType === MonitorType.Metric && (
              <MetricFields
                values={{
                  minValue: newMinValue,
                  maxValue: newMaxValue,
                  thresholdStrategy: newThresholdStrategy,
                  thresholdCount: newThresholdCount,
                  retentionDays: newRetentionDays,
                }}
                onChange={(field, value) => {
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  const setters: Record<string, (v: any) => void> = {
                    minValue: setNewMinValue,
                    maxValue: setNewMaxValue,
                    thresholdStrategy: setNewThresholdStrategy,
                    thresholdCount: setNewThresholdCount,
                    retentionDays: setNewRetentionDays,
                  };
                  setters[field]?.(value);
                }}
              />
            )}
            {newType === MonitorType.HealthCheck && (
              <HealthCheckFields
                values={{
                  healthCheckUrl: newHealthCheckUrl,
                  httpMethod: newHttpMethod,
                  expectedStatusCodes: newExpectedStatusCodes,
                  timeoutSeconds: newTimeoutSeconds,
                  bodyMatchRegex: newBodyMatchRegex,
                }}
                onChange={(field, value) => {
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  const setters: Record<string, (v: any) => void> = {
                    healthCheckUrl: setNewHealthCheckUrl,
                    httpMethod: setNewHttpMethod,
                    expectedStatusCodes: setNewExpectedStatusCodes,
                    timeoutSeconds: setNewTimeoutSeconds,
                    bodyMatchRegex: setNewBodyMatchRegex,
                  };
                  setters[field]?.(value ?? '');
                }}
                urlRequired
              />
            )}
            <Button type="button" size="sm" onClick={handleAdd} disabled={isAdding}>
              {isAdding ? 'Adding...' : 'Add'}
            </Button>
          </div>
        )}

        <div className="space-y-3">
          {monitors.map((monitor) => (
            <MonitorRow
              key={monitor.id}
              monitor={monitor}
              canDelete={monitors.length > 1}
              onUpdate={(data) => onUpdate(monitor.id, data)}
              onDelete={() => onDelete(monitor.id)}
            />
          ))}
        </div>
      </CardContent>
    </Card>
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

  const serverSet = new Set(assignedContacts?.map((c) => c.id) ?? []);
  const selected = localSelected ?? serverSet;
  const dirty = localSelected !== null;

  const saveMutation = useMutation({
    mutationFn: (contactIds: string[]) => contactsApi.setServiceContacts(serviceId, contactIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services', serviceId, 'contacts'] });
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      setLocalSelected(null);
      toast.success('Contacts updated');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update contacts'));
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
      <Card className="mt-6 py-0">
        <CardHeader>
          <CardTitle className="text-lg">Contacts</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <Skeleton className="h-5 w-full rounded" />
            <Skeleton className="h-5 w-full rounded" />
            <Skeleton className="h-5 w-full rounded" />
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="mt-6 py-0">
      <CardContent className="p-6">
        <h2 className="text-lg font-semibold text-foreground mb-2">Contacts</h2>
        <p className="text-xs text-muted-foreground mb-4">
          Select which contacts receive alerts for this service. If none are assigned, the default
          contact is used.
        </p>

        {!allContacts || allContacts.length === 0 ? (
          <p className="text-sm text-muted-foreground">No contacts configured yet.</p>
        ) : (
          <div className="space-y-2">
            {allContacts.map((contact) => (
              <Label key={contact.id} className="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={selected.has(contact.id)}
                  onCheckedChange={() => toggle(contact.id)}
                />
                <span className="text-sm text-foreground">{contact.name}</span>
                {contact.isDefault && (
                  <span className="text-xs text-muted-foreground">(default)</span>
                )}
                {contact.channels.length === 0 && (
                  <span className="text-xs text-amber-600 dark:text-amber-400">(no channels)</span>
                )}
              </Label>
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
            <Button variant="secondary" size="sm" onClick={() => setLocalSelected(null)}>
              Cancel
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
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
  const [thresholdStrategy, setThresholdStrategy] = useState<ThresholdStrategy>(
    monitor.thresholdStrategy ?? ThresholdStrategy.Immediate,
  );
  const [thresholdCount, setThresholdCount] = useState(monitor.thresholdCount ?? 3);
  const [retentionDays, setRetentionDays] = useState(monitor.retentionDays ?? 7);
  const [healthCheckUrl, setHealthCheckUrl] = useState(monitor.healthCheckUrl ?? '');
  const [httpMethod, setHttpMethod] = useState(monitor.httpMethod ?? 'GET');
  const [expectedStatusCodes, setExpectedStatusCodes] = useState(
    monitor.expectedStatusCodes ?? '200',
  );
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
      if (
        thresholdStrategy === ThresholdStrategy.ConsecutiveCount ||
        thresholdStrategy === ThresholdStrategy.SampleCountAverage
      ) {
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
        <span className="text-sm font-medium text-foreground">{typeLabels[monitor.type]}</span>
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
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="xs"
                className="p-0 h-auto text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300 hover:bg-transparent"
                disabled={!canDelete}
              >
                Remove
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Remove this monitor?</AlertDialogTitle>
                <AlertDialogDescription>
                  This will permanently remove the monitor from this service.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction onClick={() => onDelete()}>Remove</AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </div>

      {editing ? (
        <div className="space-y-2">
          <IntervalGraceFields
            intervalSeconds={interval}
            gracePeriodSeconds={grace}
            onIntervalChange={setInterval}
            onGracePeriodChange={setGrace}
          />
          {monitor.type === MonitorType.Metric && (
            <MetricFields
              values={{
                minValue,
                maxValue,
                thresholdStrategy,
                thresholdCount,
                retentionDays,
              }}
              onChange={(field, value) => {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                const setters: Record<string, (v: any) => void> = {
                  minValue: setMinValue,
                  maxValue: setMaxValue,
                  thresholdStrategy: setThresholdStrategy,
                  thresholdCount: setThresholdCount,
                  retentionDays: setRetentionDays,
                };
                setters[field]?.(value);
              }}
            />
          )}
          {monitor.type === MonitorType.HealthCheck && (
            <HealthCheckFields
              values={{
                healthCheckUrl,
                httpMethod,
                expectedStatusCodes,
                timeoutSeconds,
                bodyMatchRegex,
              }}
              onChange={(field, value) => {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                const setters: Record<string, (v: any) => void> = {
                  healthCheckUrl: setHealthCheckUrl,
                  httpMethod: setHttpMethod,
                  expectedStatusCodes: setExpectedStatusCodes,
                  timeoutSeconds: setTimeoutSeconds,
                  bodyMatchRegex: setBodyMatchRegex,
                };
                setters[field]?.(value ?? '');
              }}
            />
          )}
          <div className="flex gap-2">
            <Button type="button" size="xs" onClick={handleSave}>
              Save
            </Button>
            <Button type="button" variant="secondary" size="xs" onClick={handleCancel}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <div className="text-xs text-muted-foreground">
          Interval: {monitor.intervalSeconds}s | Grace: {monitor.gracePeriodSeconds}s
          {monitor.type === MonitorType.Metric && (
            <span>
              {monitor.minValue != null && ` | Min: ${monitor.minValue}`}
              {monitor.maxValue != null && ` | Max: ${monitor.maxValue}`}
              {monitor.thresholdStrategy != null &&
                ` | ${ThresholdStrategy[monitor.thresholdStrategy]}`}
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
