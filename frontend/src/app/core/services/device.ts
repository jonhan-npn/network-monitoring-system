import { HttpParams } from '@angular/common/http';
import { DeviceQuery, CreateDeviceRequest, Devices, UpdateDeviceRequest } from '../models/device.model';
import { PagedResult } from '../models/event-log.model';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class Device {
  private readonly apiUrl = `${environment.apiUrl}/devices`;

  constructor(private readonly http: HttpClient) {}

  getAll(query: DeviceQuery = {}): Observable<PagedResult<Devices>> {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 25));

    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.type) params = params.set('type', query.type);
    if (query.isMonitored != null) params = params.set('isMonitored', String(query.isMonitored));

    return this.http.get<PagedResult<Devices>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<Devices> {
    return this.http.get<Devices>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateDeviceRequest): Observable<Devices> {
    return this.http.post<Devices>(this.apiUrl, request);
  }

  update(id: string, request: UpdateDeviceRequest): Observable<Devices> {
    return this.http.put<Devices>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  setMonitoring(id: string, isMonitored: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/monitoring`, { isMonitored });
  }
}
