export enum DeviceType {
  Router = 'Router',
  Switch = 'Switch',
  Server = 'Server',
  Firewall = 'Firewall',
  AccessPoint = 'AccessPoint',
  Other = 'Other',
}

export enum DeviceStatus {
  Online = 'Online',
  Offline = 'Offline',
  Unknown = 'Unknown',
}

export interface Devices {
  id: string;
  name: string;
  ipAddress: string;
  deviceType: DeviceType;
  location?: string;
  description?: string;
  isMonitored: boolean;
  isOnline?: boolean;
  lastCheckedAt?: string;
  lastCpuPercent?: number;
  lastMemoryPercent?: number;
  createdAt: string;
}

export interface CreateDeviceRequest {
  name: string;
  ipAddress: string;
  deviceType: DeviceType;
  location?: string;
  description?: string;
}

export interface UpdateDeviceRequest {
  name: string;
  ipAddress: string;
  deviceType: DeviceType;
  location?: string;
  description?: string;
}

// A single CPU/RAM sample pulled by the SNMP worker, used by monitoring.service
// for resource-history charts.
export interface ResourceSample {
  cpuPercent?: number;
  memoryPercent?: number;
  sampledAt: string;
}
