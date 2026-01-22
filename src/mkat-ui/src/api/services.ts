import { api } from './client';
import type {
  Service,
  PagedResponse,
  CreateServiceRequest,
  UpdateServiceRequest,
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
};
