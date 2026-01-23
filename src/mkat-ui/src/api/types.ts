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

export interface UpdateMonitorRequest {
  intervalSeconds: number;
  gracePeriodSeconds?: number;
}
