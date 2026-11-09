export enum AlertChannel {
  Email = 'Email',
  Telegram = 'Telegram',
}

export enum AlertMetric {
  Availability = 'Availability',
  Cpu = 'Cpu',
  Memory = 'Memory',
}

export const ALERT_METRIC_LABELS: Record<string, string> = {
  [AlertMetric.Availability]: 'Device offline',
  [AlertMetric.Cpu]: 'CPU usage',
  [AlertMetric.Memory]: 'Memory usage',
};

export const ALERT_CHANNEL_LABELS: Record<string, string> = {
  [AlertChannel.Email]: 'Email',
  [AlertChannel.Telegram]: 'Telegram',
};

export interface AlertRule {
  id: string;
  /** null/undefined = the rule applies to every device. */
  deviceId?: string | null;
  deviceName?: string | null;
  name: string;
  metric: AlertMetric | string;
  /** Only meaningful for Cpu/Memory; null for Availability rules. */
  thresholdPercent?: number | null;
  consecutiveBreaches: number;
  channel: AlertChannel | string;
  destination: string;
  isEnabled: boolean;
  lastTriggeredAt?: string | null;
}

export interface CreateAlertRuleRequest {
  name: string;
  deviceId?: string | null;
  metric: AlertMetric | string;
  thresholdPercent?: number | null;
  consecutiveBreaches: number;
  channel: AlertChannel | string;
  destination: string;
}

export interface UpdateAlertRuleRequest extends CreateAlertRuleRequest {
  isEnabled: boolean;
}

export interface TestAlertRequest {
  channel: AlertChannel | string;
  destination: string;
}

export interface AlertHistoryEntry {
  id: string | number;
  ruleId: string;
  ruleName: string;
  deviceId?: string | null;
  deviceName?: string | null;
  message: string;
  channel: string;
  triggeredAt: string;
}

// Live payload pushed over SignalR by the API's MonitoringHub when the
// Worker's AlertThresholdEngine dispatches an alert.
export interface AlertUpdate {
  ruleId: string;
  deviceId?: string;
  metric: string;
  message: string;
  timestampUtc: string;
}
