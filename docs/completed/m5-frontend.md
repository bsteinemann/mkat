# Implementation Plan: M5 Frontend

**Milestone:** 5 - Frontend
**Goal:** React UI for service management and monitoring
**Dependencies:** M4 Notifications (can start earlier with mock data)

---

## 1. Project Setup

### 1.1 Create React Project

```bash
cd src
npm create vite@latest mkat-ui -- --template react-ts
cd mkat-ui
npm install
```

### 1.2 Install Dependencies

```bash
npm install @tanstack/react-router @tanstack/react-query
npm install tailwindcss postcss autoprefixer
npm install @headlessui/react @heroicons/react
npm install date-fns
npm install -D @types/node
```

### 1.3 Configure Tailwind

```bash
npx tailwindcss init -p
```

**File:** `tailwind.config.js`
```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

**File:** `src/index.css`
```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

---

## 2. Directory Structure

```
src/mkat-ui/
├── src/
│   ├── api/
│   │   ├── client.ts
│   │   ├── services.ts
│   │   ├── alerts.ts
│   │   └── types.ts
│   ├── components/
│   │   ├── layout/
│   │   │   ├── Header.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   └── Layout.tsx
│   │   ├── services/
│   │   │   ├── ServiceCard.tsx
│   │   │   ├── ServiceForm.tsx
│   │   │   ├── ServiceList.tsx
│   │   │   └── StateIndicator.tsx
│   │   ├── alerts/
│   │   │   ├── AlertItem.tsx
│   │   │   └── AlertList.tsx
│   │   └── common/
│   │       ├── Button.tsx
│   │       ├── Modal.tsx
│   │       ├── CopyableUrl.tsx
│   │       └── Pagination.tsx
│   ├── pages/
│   │   ├── Dashboard.tsx
│   │   ├── Services.tsx
│   │   ├── ServiceDetail.tsx
│   │   ├── ServiceCreate.tsx
│   │   ├── ServiceEdit.tsx
│   │   ├── Alerts.tsx
│   │   └── Login.tsx
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   └── useServices.ts
│   ├── router.tsx
│   ├── App.tsx
│   └── main.tsx
├── index.html
├── vite.config.ts
└── package.json
```

---

## 3. API Client

### 3.1 Types

**File:** `src/api/types.ts`
```typescript
export enum ServiceState {
  Unknown = 0,
  Up = 1,
  Down = 2,
  Paused = 3,
}

export enum MonitorType {
  Webhook = 0,
  Heartbeat = 1,
  HealthCheck = 2,
}

export enum AlertType {
  Failure = 0,
  Recovery = 1,
  MissedHeartbeat = 2,
}

export enum Severity {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export interface Monitor {
  id: string;
  type: MonitorType;
  token: string;
  intervalSeconds: number;
  gracePeriodSeconds: number;
  lastCheckIn: string | null;
  webhookFailUrl: string;
  webhookRecoverUrl: string;
  heartbeatUrl: string;
}

export interface Service {
  id: string;
  name: string;
  description: string | null;
  state: ServiceState;
  severity: Severity;
  pausedUntil: string | null;
  createdAt: string;
  updatedAt: string;
  monitors: Monitor[];
}

export interface Alert {
  id: string;
  serviceId: string;
  type: AlertType;
  severity: Severity;
  message: string;
  createdAt: string;
  acknowledgedAt: string | null;
  dispatchedAt: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateServiceRequest {
  name: string;
  description?: string;
  severity: Severity;
  monitors: CreateMonitorRequest[];
}

export interface CreateMonitorRequest {
  type: MonitorType;
  intervalSeconds: number;
  gracePeriodSeconds?: number;
}

export interface UpdateServiceRequest {
  name: string;
  description?: string;
  severity: Severity;
}
```

### 3.2 API Client

**File:** `src/api/client.ts`
```typescript
const API_BASE = '/api/v1';

export class ApiError extends Error {
  constructor(public status: number, public code: string, message: string) {
    super(message);
  }
}

function getAuthHeader(): string {
  const credentials = localStorage.getItem('mkat_credentials');
  if (!credentials) throw new ApiError(401, 'UNAUTHORIZED', 'Not authenticated');
  return `Basic ${credentials}`;
}

export async function apiRequest<T>(
  method: string,
  path: string,
  body?: unknown
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': getAuthHeader(),
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401) {
    localStorage.removeItem('mkat_credentials');
    window.location.href = '/login';
    throw new ApiError(401, 'UNAUTHORIZED', 'Session expired');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new ApiError(response.status, error.code || 'ERROR', error.error);
  }

  if (response.status === 204) return {} as T;
  return response.json();
}

export const api = {
  get: <T>(path: string) => apiRequest<T>('GET', path),
  post: <T>(path: string, body?: unknown) => apiRequest<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => apiRequest<T>('PUT', path, body),
  delete: <T>(path: string) => apiRequest<T>('DELETE', path),
};
```

### 3.3 Service API

**File:** `src/api/services.ts`
```typescript
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
```

---

## 4. Components

### 4.1 State Indicator

**File:** `src/components/services/StateIndicator.tsx`
```typescript
import { ServiceState } from '../../api/types';

interface Props {
  state: ServiceState;
  size?: 'sm' | 'md' | 'lg';
}

const stateConfig = {
  [ServiceState.Up]: { color: 'bg-green-500', label: 'Up', pulse: false },
  [ServiceState.Down]: { color: 'bg-red-500', label: 'Down', pulse: true },
  [ServiceState.Paused]: { color: 'bg-yellow-500', label: 'Paused', pulse: false },
  [ServiceState.Unknown]: { color: 'bg-gray-400', label: 'Unknown', pulse: false },
};

const sizes = {
  sm: 'h-2 w-2',
  md: 'h-3 w-3',
  lg: 'h-4 w-4',
};

export function StateIndicator({ state, size = 'md' }: Props) {
  const config = stateConfig[state];

  return (
    <span className="flex items-center gap-2">
      <span className={`relative inline-flex ${sizes[size]}`}>
        {config.pulse && (
          <span className={`animate-ping absolute inline-flex h-full w-full rounded-full ${config.color} opacity-75`} />
        )}
        <span className={`relative inline-flex rounded-full ${sizes[size]} ${config.color}`} />
      </span>
      <span className="text-sm font-medium">{config.label}</span>
    </span>
  );
}
```

### 4.2 Service Card

**File:** `src/components/services/ServiceCard.tsx`
```typescript
import { Link } from '@tanstack/react-router';
import { Service, Severity } from '../../api/types';
import { StateIndicator } from './StateIndicator';
import { formatDistanceToNow } from 'date-fns';

interface Props {
  service: Service;
  onPause?: () => void;
  onResume?: () => void;
}

const severityColors = {
  [Severity.Low]: 'border-green-200',
  [Severity.Medium]: 'border-yellow-200',
  [Severity.High]: 'border-orange-200',
  [Severity.Critical]: 'border-red-200',
};

export function ServiceCard({ service, onPause, onResume }: Props) {
  return (
    <div className={`bg-white rounded-lg shadow p-4 border-l-4 ${severityColors[service.severity]}`}>
      <div className="flex items-start justify-between">
        <div>
          <Link
            to="/services/$serviceId"
            params={{ serviceId: service.id }}
            className="text-lg font-semibold text-gray-900 hover:text-blue-600"
          >
            {service.name}
          </Link>
          {service.description && (
            <p className="text-sm text-gray-500 mt-1">{service.description}</p>
          )}
        </div>
        <StateIndicator state={service.state} />
      </div>

      <div className="mt-4 flex items-center justify-between text-sm text-gray-500">
        <span>Updated {formatDistanceToNow(new Date(service.updatedAt))} ago</span>
        <div className="flex gap-2">
          {service.state !== 3 ? (
            <button
              onClick={onPause}
              className="text-yellow-600 hover:text-yellow-800"
            >
              Pause
            </button>
          ) : (
            <button
              onClick={onResume}
              className="text-green-600 hover:text-green-800"
            >
              Resume
            </button>
          )}
          <Link
            to="/services/$serviceId/edit"
            params={{ serviceId: service.id }}
            className="text-blue-600 hover:text-blue-800"
          >
            Edit
          </Link>
        </div>
      </div>
    </div>
  );
}
```

### 4.3 Copyable URL

**File:** `src/components/common/CopyableUrl.tsx`
```typescript
import { useState } from 'react';
import { ClipboardIcon, CheckIcon } from '@heroicons/react/24/outline';

interface Props {
  label: string;
  url: string;
}

export function CopyableUrl({ label, url }: Props) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col gap-1">
      <label className="text-sm font-medium text-gray-700">{label}</label>
      <div className="flex items-center gap-2">
        <code className="flex-1 bg-gray-100 px-3 py-2 rounded text-sm font-mono truncate">
          {url}
        </code>
        <button
          onClick={handleCopy}
          className="p-2 text-gray-500 hover:text-gray-700 rounded hover:bg-gray-100"
          title={copied ? 'Copied!' : 'Copy to clipboard'}
        >
          {copied ? (
            <CheckIcon className="h-5 w-5 text-green-500" />
          ) : (
            <ClipboardIcon className="h-5 w-5" />
          )}
        </button>
      </div>
    </div>
  );
}
```

---

## 5. Pages

### 5.1 Dashboard

**File:** `src/pages/Dashboard.tsx`
```typescript
import { useQuery } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { alertsApi } from '../api/alerts';
import { ServiceState } from '../api/types';
import { StateIndicator } from '../components/services/StateIndicator';
import { AlertItem } from '../components/alerts/AlertItem';

export function Dashboard() {
  const { data: servicesData } = useQuery({
    queryKey: ['services'],
    queryFn: () => servicesApi.list(1, 100),
    refetchInterval: 30000,
  });

  const { data: alertsData } = useQuery({
    queryKey: ['alerts', 'recent'],
    queryFn: () => alertsApi.list(1, 5),
    refetchInterval: 30000,
  });

  const services = servicesData?.items ?? [];
  const alerts = alertsData?.items ?? [];

  const counts = {
    up: services.filter(s => s.state === ServiceState.Up).length,
    down: services.filter(s => s.state === ServiceState.Down).length,
    paused: services.filter(s => s.state === ServiceState.Paused).length,
    unknown: services.filter(s => s.state === ServiceState.Unknown).length,
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {/* Status Overview */}
      <div className="grid grid-cols-4 gap-4">
        <StatCard state={ServiceState.Up} count={counts.up} />
        <StatCard state={ServiceState.Down} count={counts.down} />
        <StatCard state={ServiceState.Paused} count={counts.paused} />
        <StatCard state={ServiceState.Unknown} count={counts.unknown} />
      </div>

      {/* Recent Alerts */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold mb-4">Recent Alerts</h2>
        {alerts.length === 0 ? (
          <p className="text-gray-500">No recent alerts</p>
        ) : (
          <div className="space-y-3">
            {alerts.map(alert => (
              <AlertItem key={alert.id} alert={alert} />
            ))}
          </div>
        )}
      </div>

      {/* Services needing attention */}
      {counts.down > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold text-red-800 mb-4">
            Services Down ({counts.down})
          </h2>
          <div className="space-y-2">
            {services
              .filter(s => s.state === ServiceState.Down)
              .map(s => (
                <div key={s.id} className="flex items-center justify-between">
                  <span className="font-medium">{s.name}</span>
                  <StateIndicator state={s.state} size="sm" />
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({ state, count }: { state: ServiceState; count: number }) {
  const labels = {
    [ServiceState.Up]: 'Up',
    [ServiceState.Down]: 'Down',
    [ServiceState.Paused]: 'Paused',
    [ServiceState.Unknown]: 'Unknown',
  };

  return (
    <div className="bg-white rounded-lg shadow p-4">
      <div className="flex items-center justify-between">
        <StateIndicator state={state} />
        <span className="text-3xl font-bold">{count}</span>
      </div>
      <p className="text-sm text-gray-500 mt-2">{labels[state]} Services</p>
    </div>
  );
}
```

### 5.2 Service Detail

**File:** `src/pages/ServiceDetail.tsx`
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from '@tanstack/react-router';
import { servicesApi } from '../api/services';
import { alertsApi } from '../api/alerts';
import { MonitorType } from '../api/types';
import { StateIndicator } from '../components/services/StateIndicator';
import { CopyableUrl } from '../components/common/CopyableUrl';
import { AlertItem } from '../components/alerts/AlertItem';

export function ServiceDetail() {
  const { serviceId } = useParams({ from: '/services/$serviceId' });
  const queryClient = useQueryClient();

  const { data: service, isLoading } = useQuery({
    queryKey: ['services', serviceId],
    queryFn: () => servicesApi.get(serviceId),
  });

  const { data: alertsData } = useQuery({
    queryKey: ['alerts', 'service', serviceId],
    queryFn: () => alertsApi.listByService(serviceId, 1, 10),
  });

  const pauseMutation = useMutation({
    mutationFn: () => servicesApi.pause(serviceId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const resumeMutation = useMutation({
    mutationFn: () => servicesApi.resume(serviceId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  if (isLoading || !service) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{service.name}</h1>
          {service.description && (
            <p className="text-gray-500 mt-1">{service.description}</p>
          )}
        </div>
        <div className="flex items-center gap-4">
          <StateIndicator state={service.state} size="lg" />
          <div className="flex gap-2">
            {service.state !== 3 ? (
              <button
                onClick={() => pauseMutation.mutate()}
                className="px-4 py-2 bg-yellow-100 text-yellow-800 rounded hover:bg-yellow-200"
              >
                Pause
              </button>
            ) : (
              <button
                onClick={() => resumeMutation.mutate()}
                className="px-4 py-2 bg-green-100 text-green-800 rounded hover:bg-green-200"
              >
                Resume
              </button>
            )}
            <Link
              to="/services/$serviceId/edit"
              params={{ serviceId }}
              className="px-4 py-2 bg-blue-100 text-blue-800 rounded hover:bg-blue-200"
            >
              Edit
            </Link>
          </div>
        </div>
      </div>

      {/* Monitors */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold mb-4">Monitors</h2>
        <div className="space-y-6">
          {service.monitors.map(monitor => (
            <div key={monitor.id} className="border-b pb-4 last:border-0">
              <div className="flex items-center gap-2 mb-3">
                <span className="font-medium">
                  {monitor.type === MonitorType.Webhook ? 'Webhook' : 'Heartbeat'}
                </span>
                <span className="text-sm text-gray-500">
                  ({monitor.intervalSeconds}s interval)
                </span>
              </div>

              {monitor.type === MonitorType.Webhook ? (
                <div className="space-y-3">
                  <CopyableUrl label="Failure URL" url={monitor.webhookFailUrl} />
                  <CopyableUrl label="Recovery URL" url={monitor.webhookRecoverUrl} />
                </div>
              ) : (
                <CopyableUrl label="Heartbeat URL" url={monitor.heartbeatUrl} />
              )}

              {monitor.lastCheckIn && (
                <p className="text-sm text-gray-500 mt-2">
                  Last check-in: {new Date(monitor.lastCheckIn).toLocaleString()}
                </p>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Alert History */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold mb-4">Recent Alerts</h2>
        {alertsData?.items.length === 0 ? (
          <p className="text-gray-500">No alerts for this service</p>
        ) : (
          <div className="space-y-3">
            {alertsData?.items.map(alert => (
              <AlertItem key={alert.id} alert={alert} showService={false} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
```

### 5.3 Login Page

**File:** `src/pages/Login.tsx`
```typescript
import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';

export function Login() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    const credentials = btoa(`${username}:${password}`);

    try {
      const response = await fetch('/api/v1/services?page=1&pageSize=1', {
        headers: { Authorization: `Basic ${credentials}` },
      });

      if (response.ok) {
        localStorage.setItem('mkat_credentials', credentials);
        navigate({ to: '/' });
      } else {
        setError('Invalid credentials');
      }
    } catch {
      setError('Connection failed');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h1 className="text-2xl font-bold text-center mb-6">mkat</h1>

        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">
              Username
            </label>
            <input
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              className="mt-1 block w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              className="mt-1 block w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              required
            />
          </div>

          <button
            type="submit"
            className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
          >
            Login
          </button>
        </form>
      </div>
    </div>
  );
}
```

---

## 6. Router Setup

**File:** `src/router.tsx`
```typescript
import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  redirect,
} from '@tanstack/react-router';
import { Layout } from './components/layout/Layout';
import { Dashboard } from './pages/Dashboard';
import { Services } from './pages/Services';
import { ServiceDetail } from './pages/ServiceDetail';
import { ServiceCreate } from './pages/ServiceCreate';
import { ServiceEdit } from './pages/ServiceEdit';
import { Alerts } from './pages/Alerts';
import { Login } from './pages/Login';

const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: Login,
});

const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'authenticated',
  beforeLoad: () => {
    if (!localStorage.getItem('mkat_credentials')) {
      throw redirect({ to: '/login' });
    }
  },
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
});

const dashboardRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/',
  component: Dashboard,
});

const servicesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services',
  component: Services,
});

const serviceDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/$serviceId',
  component: ServiceDetail,
});

const serviceCreateRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/new',
  component: ServiceCreate,
});

const serviceEditRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/$serviceId/edit',
  component: ServiceEdit,
});

const alertsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/alerts',
  component: Alerts,
});

const routeTree = rootRoute.addChildren([
  loginRoute,
  authenticatedRoute.addChildren([
    dashboardRoute,
    servicesRoute,
    serviceDetailRoute,
    serviceCreateRoute,
    serviceEditRoute,
    alertsRoute,
  ]),
]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
```

---

## 7. Vite Configuration (Proxy)

**File:** `vite.config.ts`
```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/webhook': 'http://localhost:5000',
      '/heartbeat': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
  build: {
    outDir: '../Mkat.Api/wwwroot',
    emptyOutDir: true,
  },
});
```

---

## 8. Serve Static Files from API

**Update:** `src/Mkat.Api/Program.cs`
```csharp
// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");
```

---

## 9. Verification Checklist

- [ ] Login page authenticates against API
- [ ] Dashboard shows service state counts
- [ ] Dashboard shows recent alerts
- [ ] Services page lists all services
- [ ] Service detail shows monitors with URLs
- [ ] Create service form validates input
- [ ] Edit service form pre-fills data
- [ ] Pause/resume works from UI
- [ ] Mute modal works
- [ ] Alerts page shows alert history
- [ ] Alert acknowledgment works
- [ ] Auto-refresh updates data
- [ ] UI builds and serves from API
- [ ] Mobile responsive layout

---

## 10. Files to Create

| File | Purpose |
|------|---------|
| `src/mkat-ui/` | React project root |
| `src/mkat-ui/src/api/*.ts` | API client and types |
| `src/mkat-ui/src/components/**/*.tsx` | Reusable components |
| `src/mkat-ui/src/pages/*.tsx` | Page components |
| `src/mkat-ui/src/router.tsx` | Router configuration |
| `src/mkat-ui/src/App.tsx` | App entry point |
| `src/mkat-ui/vite.config.ts` | Build configuration |
| `src/mkat-ui/tailwind.config.js` | Tailwind configuration |

---

**Status:** Ready for implementation
**Estimated complexity:** High
