import { getBasePath } from '../config';

function getApiBase(): string {
  return `${getBasePath()}/api/v1`;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
    public details?: Record<string, string[]>,
  ) {
    super(message);
  }

  get userMessage(): string {
    if (this.details) {
      const msgs = Object.values(this.details).flat();
      if (msgs.length > 0) return msgs.join('. ');
    }
    return this.message;
  }
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) throw new ApiError(401, 'UNAUTHORIZED', 'Not authenticated');
  return `Basic ${credentials}`;
}

export async function apiRequest<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${getApiBase()}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: getAuthHeader(),
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401) {
    localStorage.removeItem('mkat_credentials');
    window.location.href = `${getBasePath()}/login`;
    throw new ApiError(401, 'UNAUTHORIZED', 'Session expired');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new ApiError(response.status, error.code || 'ERROR', error.error, error.details);
  }

  if (response.status === 204) return {} as T;
  return response.json();
}

export function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) return error.userMessage;
  return fallback;
}

export const api = {
  get: <T>(path: string) => apiRequest<T>('GET', path),
  post: <T>(path: string, body?: unknown) => apiRequest<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => apiRequest<T>('PUT', path, body),
  delete: <T>(path: string) => apiRequest<T>('DELETE', path),
};
