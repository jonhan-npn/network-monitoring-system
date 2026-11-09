export interface PingResult {
  id?: string;
  deviceId?: string;
  isOnline: boolean;
  latencyMs?: number; // undefined when the ping timed out / failed
  checkedAt: string; // ISO date string
}

// Query params accepted by GET /api/monitoring/devices/{id}/ping-history
export interface PingHistoryQuery {
  deviceId: string;
  from?: string;
  to?: string;
}

// Live payload pushed over SignalR by the API's MonitoringHub after each
// ping sweep. Field names match the server's DeviceStatusUpdate record.
export interface DeviceStatusUpdate {
  deviceId: string;
  name: string;
  isOnline: boolean;
  timestampUtc: string;
  latencyMs?: number;
}
