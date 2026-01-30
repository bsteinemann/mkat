import { api } from './client';
import type {
  Service,
  Monitor,
  PagedResponse,
  CreateServiceRequest,
  CreateMonitorRequest,
  UpdateServiceRequest,
  UpdateMonitorRequest,
  MetricHistoryResponse,
  MetricLatestResponse,
  MonitorEvent,
  MonitorRollup,
  ServiceUptime,
  Peer,
  PeerInitiateResponse,
  PeerResponse,
  Contact,
  ContactChannel,
} from './types';

export const servicesApi = {
  list: (page = 1, pageSize = 20) =>
    api.get<PagedResponse<Service>>(`/services?page=${page}&pageSize=${pageSize}`),

  get: (id: string) => api.get<Service>(`/services/${id}`),

  create: (data: CreateServiceRequest) => api.post<Service>('/services', data),

  update: (id: string, data: UpdateServiceRequest) => api.put<Service>(`/services/${id}`, data),

  delete: (id: string) => api.delete<void>(`/services/${id}`),

  pause: (id: string, until?: string, autoResume = false) =>
    api.post<{ paused: boolean }>(`/services/${id}/pause`, { until, autoResume }),

  resume: (id: string) => api.post<{ resumed: boolean }>(`/services/${id}/resume`),

  mute: (id: string, durationMinutes: number, reason?: string) =>
    api.post<{ muted: boolean }>(`/services/${id}/mute`, { durationMinutes, reason }),

  addMonitor: (serviceId: string, data: CreateMonitorRequest) =>
    api.post<Monitor>(`/services/${serviceId}/monitors`, data),

  updateMonitor: (serviceId: string, monitorId: string, data: UpdateMonitorRequest) =>
    api.put<Monitor>(`/services/${serviceId}/monitors/${monitorId}`, data),

  deleteMonitor: (serviceId: string, monitorId: string) =>
    api.delete<void>(`/services/${serviceId}/monitors/${monitorId}`),
};

export const peersApi = {
  list: () => api.get<Peer[]>('/peers'),

  initiate: (name: string) => api.post<PeerInitiateResponse>('/peers/pair/initiate', { name }),

  complete: (token: string) => api.post<PeerResponse>('/peers/pair/complete', { token }),

  unpair: (id: string) => api.delete<void>(`/peers/${id}`),
};

export const contactsApi = {
  list: () => api.get<Contact[]>('/contacts'),

  get: (id: string) => api.get<Contact>(`/contacts/${id}`),

  create: (name: string) => api.post<Contact>('/contacts', { name }),

  update: (id: string, name: string) => api.put<Contact>(`/contacts/${id}`, { name }),

  delete: (id: string) => api.delete<void>(`/contacts/${id}`),

  addChannel: (contactId: string, type: number, configuration: string) =>
    api.post<ContactChannel>(`/contacts/${contactId}/channels`, { type, configuration }),

  updateChannel: (
    contactId: string,
    channelId: string,
    configuration: string,
    isEnabled: boolean,
  ) =>
    api.put<ContactChannel>(`/contacts/${contactId}/channels/${channelId}`, {
      configuration,
      isEnabled,
    }),

  deleteChannel: (contactId: string, channelId: string) =>
    api.delete<void>(`/contacts/${contactId}/channels/${channelId}`),

  testChannel: (contactId: string, channelId: string) =>
    api.post<{ success: boolean }>(`/contacts/${contactId}/channels/${channelId}/test`),

  getServiceContacts: (serviceId: string) => api.get<Contact[]>(`/services/${serviceId}/contacts`),

  setServiceContacts: (serviceId: string, contactIds: string[]) =>
    api.put<{ assigned: number }>(`/services/${serviceId}/contacts`, { contactIds }),
};

export const monitorEventsApi = {
  getByMonitor: (
    monitorId: string,
    params?: { from?: string; to?: string; eventType?: string; limit?: number },
  ) => {
    const query = new URLSearchParams();
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);
    if (params?.eventType) query.set('eventType', params.eventType);
    if (params?.limit) query.set('limit', params.limit.toString());
    const qs = query.toString();
    return api.get<MonitorEvent[]>(`/monitors/${monitorId}/events${qs ? `?${qs}` : ''}`);
  },

  getByService: (
    serviceId: string,
    params?: { from?: string; to?: string; eventType?: string; limit?: number },
  ) => {
    const query = new URLSearchParams();
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);
    if (params?.eventType) query.set('eventType', params.eventType);
    if (params?.limit) query.set('limit', params.limit.toString());
    const qs = query.toString();
    return api.get<MonitorEvent[]>(`/services/${serviceId}/events${qs ? `?${qs}` : ''}`);
  },
};

export const monitorRollupsApi = {
  getByMonitor: (
    monitorId: string,
    params?: { granularity?: string; from?: string; to?: string },
  ) => {
    const query = new URLSearchParams();
    if (params?.granularity) query.set('granularity', params.granularity);
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);
    const qs = query.toString();
    return api.get<MonitorRollup[]>(`/monitors/${monitorId}/rollups${qs ? `?${qs}` : ''}`);
  },
};

export const serviceUptimeApi = {
  get: (serviceId: string, params?: { from?: string; to?: string }) => {
    const query = new URLSearchParams();
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);
    const qs = query.toString();
    return api.get<ServiceUptime>(`/services/${serviceId}/uptime${qs ? `?${qs}` : ''}`);
  },
};

export const metricsApi = {
  getHistory: (monitorId: string, params?: { from?: string; to?: string; limit?: number }) => {
    const query = new URLSearchParams();
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);
    if (params?.limit) query.set('limit', params.limit.toString());
    const qs = query.toString();
    return api.get<MetricHistoryResponse>(`/monitors/${monitorId}/metrics${qs ? `?${qs}` : ''}`);
  },

  getLatest: (monitorId: string) =>
    api.get<MetricLatestResponse>(`/monitors/${monitorId}/metrics/latest`),
};
