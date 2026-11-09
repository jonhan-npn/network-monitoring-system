import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AlertHistoryEntry,
  AlertRule,
  CreateAlertRuleRequest,
  TestAlertRequest,
  UpdateAlertRuleRequest,
} from '../models/alert.model';
import { PagedResult } from '../models/event-log.model';

@Injectable({ providedIn: 'root' })
export class Alert {
  private readonly apiUrl = `${environment.apiUrl}/alerts`;

  constructor(private readonly http: HttpClient) {}

  getRules(): Observable<AlertRule[]> {
    return this.http.get<AlertRule[]>(`${this.apiUrl}/rules`);
  }

  createRule(request: CreateAlertRuleRequest): Observable<AlertRule> {
    return this.http.post<AlertRule>(`${this.apiUrl}/rules`, request);
  }

  updateRule(id: string, request: UpdateAlertRuleRequest): Observable<AlertRule> {
    return this.http.put<AlertRule>(`${this.apiUrl}/rules/${id}`, request);
  }

  deleteRule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/rules/${id}`);
  }

  getHistory(page = 1, pageSize = 50, deviceId?: string): Observable<PagedResult<AlertHistoryEntry>> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (deviceId) params = params.set('deviceId', deviceId);
    return this.http.get<PagedResult<AlertHistoryEntry>>(`${this.apiUrl}/history`, { params });
  }

  sendTest(request: TestAlertRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/test`, request);
  }
}
