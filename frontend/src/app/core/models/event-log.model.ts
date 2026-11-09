export enum EventType {
  DeviceOnline = 'DeviceOnline',
  DeviceOffline = 'DeviceOffline',
  DeviceCreated = 'DeviceCreated',
  DeviceUpdated = 'DeviceUpdated',
  DeviceDeleted = 'DeviceDeleted',
  AlertTriggered = 'AlertTriggered',
  UserLoggedIn = 'UserLoggedIn',
}

export const EVENT_TYPE_LABELS: Record<string, string> = {
  DeviceOnline: 'Device online',
  DeviceOffline: 'Device offline',
  DeviceCreated: 'Device created',
  DeviceUpdated: 'Device updated',
  DeviceDeleted: 'Device deleted',
  AlertTriggered: 'Alert triggered',
  UserLoggedIn: 'User signed in',
};

export interface EventLogEntry {
  id: string | number;
  eventType: string;
  message: string;
  deviceId?: string;
  deviceName?: string;
  occurredAt: string; // ISO date string
}

// Query params accepted by GET /api/monitoring/events
export interface EventLogQuery {
  deviceId?: string;
  eventType?: EventType | string;
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
