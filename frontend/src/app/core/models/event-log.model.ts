export enum EventType {
  DeviceOnline = 'DeviceOnline',
  DeviceOffline = 'DeviceOffline',
  DeviceCreated = 'DeviceCreated',
  DeviceUpdated = 'DeviceUpdated',
  DeviceDeleted = 'DeviceDeleted',
  AlertTriggered = 'AlertTriggered',
  UserLoggedIn = 'UserLoggedIn',
}

export enum EventSeverity {
  Info = 'Info',
  Warning = 'Warning',
  Critical = 'Critical',
}

export interface EventLogEntry {
  id: string | number;
  eventType: string;
  message: string;
  deviceId?: string;
  deviceName?: string;
  occurredAt: string; // ISO date string
}

// Query params accepted by GET /api/logs
export interface EventLogQuery {
  deviceId?: string;
  eventType?: EventType;
  severity?: EventSeverity;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
