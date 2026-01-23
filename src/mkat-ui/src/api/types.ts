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
  Metric = 3,
}

export enum ThresholdStrategy {
  Immediate = 0,
  ConsecutiveCount = 1,
  TimeDurationAverage = 2,
  SampleCountAverage = 3,
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
  metricUrl: string;
  minValue: number | null;
  maxValue: number | null;
  thresholdStrategy: ThresholdStrategy | null;
  thresholdCount: number | null;
  windowSeconds: number | null;
  windowSampleCount: number | null;
  retentionDays: number | null;
  lastMetricValue: number | null;
  lastMetricAt: string | null;
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
  minValue?: number;
  maxValue?: number;
  thresholdStrategy?: ThresholdStrategy;
  thresholdCount?: number;
  windowSeconds?: number;
  windowSampleCount?: number;
  retentionDays?: number;
}

export interface UpdateServiceRequest {
  name: string;
  description?: string;
  severity: Severity;
}

export interface UpdateMonitorRequest {
  type?: MonitorType;
  intervalSeconds: number;
  gracePeriodSeconds?: number;
  minValue?: number;
  maxValue?: number;
  thresholdStrategy?: ThresholdStrategy;
  thresholdCount?: number;
  windowSeconds?: number;
  windowSampleCount?: number;
  retentionDays?: number;
}

export interface MetricReading {
  id: string;
  value: number;
  recordedAt: string;
  outOfRange: boolean;
}

export interface MetricHistoryResponse {
  monitorId: string;
  readings: MetricReading[];
}

export interface MetricLatestResponse {
  value: number;
  recordedAt: string;
  outOfRange: boolean;
  monitorId: string;
}

export interface Peer {
  id: string;
  name: string;
  url: string;
  serviceId: string;
  pairedAt: string;
  heartbeatIntervalSeconds: number;
  serviceState: ServiceState | null;
}

export interface PeerInitiateRequest {
  name: string;
}

export interface PeerInitiateResponse {
  token: string;
}

export interface PeerCompleteRequest {
  token: string;
}

export interface PeerResponse {
  id: string;
  name: string;
  url: string;
  serviceId: string;
  pairedAt: string;
  heartbeatIntervalSeconds: number;
  serviceState: ServiceState | null;
}

export enum ChannelType {
  Telegram = 0,
  Email = 1,
}

export interface ContactChannel {
  id: string;
  type: ChannelType;
  configuration: string;
  isEnabled: boolean;
  createdAt: string;
}

export interface Contact {
  id: string;
  name: string;
  isDefault: boolean;
  createdAt: string;
  channels: ContactChannel[];
  serviceCount: number;
}
