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
} from './types';

export const servicesApi = {
  list: (page = 1, pageSize = 20) =>
    api.get<PagedResponse<Service>>(`/services?page=${page}&pageSize=${pageSize}`),

  get: (id: string) => api.get<Service>(`/services/${id}`),

  create: (data: CreateServiceRequest) => api.post<Service>('/services', data),

  update: (id: string, data: UpdateServiceRequest) =>
    api.put<Service>(`/services/${id}`, data),

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
