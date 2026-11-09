export enum DeviceType {
  Router = 'Router',
  Switch = 'Switch',
  Server = 'Server',
  Firewall = 'Firewall',
  AccessPoint = 'AccessPoint',
  Other = 'Other',
}

export const DEVICE_TYPE_LABELS: Record<DeviceType, string> = {
  [DeviceType.Router]: 'Router',
  [DeviceType.Switch]: 'Switch',
  [DeviceType.Server]: 'Server',
  [DeviceType.Firewall]: 'Firewall',
  [DeviceType.AccessPoint]: 'Access point',
  [DeviceType.Other]: 'Other',
};

export enum DeviceStatus {
  Online = 'Online',
  Offline = 'Offline',
  Paused = 'Paused',
  Unknown = 'Unknown',
}

export const SNMP_VERSIONS = ['1', '2c', '3'] as const;

/**
 * Device shape returned by the API. List endpoints omit the SNMP/SSH fields,
 * so they are optional here; the detail endpoint fills them in.
 */
export interface Devices {
  id: string;
  name: string;
  ipAddress: string;
  deviceType: DeviceType;
  location?: string;
  description?: string;
  snmpCommunity?: string;
  snmpPort?: number;
  snmpVersion?: string;
  sshPort?: number;
  sshUsername?: string;
  sshCredentialRef?: string;
  isMonitored: boolean;
  isOnline?: boolean;
  lastCheckedAt?: string;
  lastCpuPercent?: number;
  lastMemoryPercent?: number;
  createdAt: string;
  updatedAt?: string;
}

export interface DeviceQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  type?: DeviceType | '';
  isMonitored?: boolean;
}

export interface CreateDeviceRequest {
  name: string;
  ipAddress: string;
  deviceType: DeviceType;
  location?: string | null;
  description?: string | null;
  snmpCommunity?: string;
  snmpPort?: number;
  snmpVersion?: string;
  sshPort?: number;
  sshUsername?: string | null;
  sshCredentialRef?: string | null;
}

export type UpdateDeviceRequest = CreateDeviceRequest;

// A single CPU/RAM sample pulled by the SNMP worker, used by monitoring.service
// for resource-history charts.
export interface ResourceSample {
  cpuPercent?: number;
  memoryPercent?: number;
  sampledAt: string;
}

// Live payload pushed over SignalR by the API's MonitoringHub after each
// SNMP poll. Field names match the server's ResourceSampleUpdate record.
export interface ResourceSampleUpdate {
  deviceId: string;
  timestampUtc: string;
  cpuPercent?: number;
  ramPercent?: number;
}
