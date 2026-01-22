import { api } from './client';
import type { Alert, PagedResponse } from './types';

export const alertsApi = {
  list: (page = 1, pageSize = 20) =>
    api.get<PagedResponse<Alert>>(`/alerts?page=${page}&pageSize=${pageSize}`),

  get: (id: string) => api.get<Alert>(`/alerts/${id}`),

  acknowledge: (id: string) => api.post<{ acknowledged: boolean }>(`/alerts/${id}/ack`),
};
