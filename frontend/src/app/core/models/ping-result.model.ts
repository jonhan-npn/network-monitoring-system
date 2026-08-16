export interface PingResult {
  id: string;
  deviceId: string;
  isOnline: boolean;
  latencyMs?: number; // undefined when the ping timed out / failed
  checkedAt: string; // ISO date string
}

// Query params accepted by GET /api/monitoring/ping-history
export interface PingHistoryQuery {
  deviceId: string;
  from?: string;
  to?: string;
  take?: number;
}

// Lightweight live-status payload pushed over SignalR on every 30s sweep.
export interface DeviceStatusUpdate {
  deviceId: string;
  status: 'Online' | 'Offline';
  latencyMs?: number;
  checkedAt: string;
}
