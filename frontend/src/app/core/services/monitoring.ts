import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PingResult } from '../models/ping-result.model';
import { ResourceSample } from '../models/device.model';
import { EventLogEntry, EventLogQuery, PagedResult } from '../models/event-log.model';

export interface DashboardSummary {
  totalDevices: number;
  onlineDevices: number;
  offlineDevices: number;
  averageCpuPercent: number;
  averageMemoryPercent: number;
  activeAlertRules: number;
  alertsTriggeredLast24h: number;
}

export interface DeviceStatus {
  deviceId: string;
  deviceName: string;
  isOnline: boolean;
  latencyMs?: number;
  checkedAt: string;
}

@Injectable({ providedIn: 'root' })
export class Monitoring {
  private readonly apiUrl = `${environment.apiUrl}/monitoring`;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard/summary`);
  }

  getStatus(): Observable<DeviceStatus[]> {
    return this.http.get<DeviceStatus[]>(`${this.apiUrl}/status`);
  }

  getPingHistory(deviceId: string, from?: string, to?: string): Observable<PingResult[]> {
    return this.http.get<PingResult[]>(`${this.apiUrl}/devices/${deviceId}/ping-history`, {
      params: this.dates(from, to),
    });
  }

  getResourceHistory(deviceId: string, from?: string, to?: string): Observable<ResourceSample[]> {
    return this.http.get<ResourceSample[]>(`${this.apiUrl}/devices/${deviceId}/resource-history`, {
      params: this.dates(from, to),
    });
  }

  getEvents(query: EventLogQuery = {}): Observable<PagedResult<EventLogEntry>> {
    return this.http.get<PagedResult<EventLogEntry>>(`${this.apiUrl}/events`, {
      params: Object.entries(query).reduce(
        (params, [key, value]) => (value == null || value === '' ? params : params.set(key, String(value))),
        new HttpParams(),
      ),
    });
  }

  private dates(from?: string, to?: string): HttpParams {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return params;
  }
}
