import { Component, signal } from '@angular/core';
import { DashboardSummary } from '../../../core/services/monitoring';

@Component({
  selector: 'app-device-status-chart',
  imports: [],
  templateUrl: './device-status-chart.html',
  styleUrl: './device-status-chart.css',
})
export class DeviceStatusChart {
  readonly summary = signal<DashboardSummary | null>(null);
  
  totalDevices() { return this.summary()?.totalDevices ?? this.fallbackMetrics.totalDevices; }
  onlineDevices() { return this.summary()?.onlineDevices ?? this.fallbackMetrics.onlineDevices; }
    offlineDevices() { return this.summary()?.offlineDevices ?? this.fallbackMetrics.offlineDevices; }
  warningDevices() { return Math.max(0, this.totalDevices() - this.onlineDevices() - this.offlineDevices()); }
  private readonly fallbackMetrics = {
    totalDevices: 48,
    onlineDevices: 44,
    offlineDevices: 2,
    activeAlerts: 3,
    uptime: 99.98,
  };
  onlinePercentage() {
    const total = this.totalDevices();
    return total ? Number(((this.onlineDevices() / total) * 100).toFixed(1)) : 0;
  }
  get deviceHealth() {
    const circumference = 2 * Math.PI * 40;
    const total = this.totalDevices() || 1;
    const dash = (value: number) => `${(circumference * value / total).toFixed(2)} ${circumference.toFixed(2)}`;
    const online = this.onlineDevices();
    const warning = this.warningDevices();
    return {
      onlineDash: dash(online),
      warningDash: dash(warning),
      warningOffset: `-${(circumference * online / total).toFixed(2)}`,
      offlineDash: dash(this.offlineDevices()),
      offlineOffset: `-${(circumference * (online + warning) / total).toFixed(2)}`,
    };
  }
}
