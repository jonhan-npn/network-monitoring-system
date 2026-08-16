export enum AlertChannel {
  Email = 'Email',
  Telegram = 'Telegram',
}

export enum AlertMetric {
  DeviceOffline = 'DeviceOffline',
  CpuAbovePercent = 'CpuAbovePercent',
  RamAbovePercent = 'RamAbovePercent',
}

export interface AlertRule {
  id: string;
  name: string;
  deviceId?: string; // undefined = applies to all devices
  metric: string;
  thresholdPercent?: number;
  consecutiveBreaches: number;
  channel: string;
  destination: string;
  isEnabled: boolean;
}

export interface CreateAlertRuleRequest {
  name: string;
  deviceId?: string;
  metricType: string;
  comparator: string;
  threshold: number;
  durationSeconds: number;
  channel: string;
}

export interface UpdateAlertRuleRequest { name: string; comparator: string; threshold: number; durationSeconds: number; channel: string; isEnabled: boolean; }

export interface AlertHistoryEntry {
  id: string | number;
  ruleId: string;
  ruleName: string;
  deviceId?: string;
  deviceName?: string;
  message: string;
  channel: string;
  triggeredAt: string;
}
