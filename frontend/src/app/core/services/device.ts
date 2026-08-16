import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { CreateDeviceRequest,Devices, UpdateDeviceRequest } from '../models/device.model';
import { PagedResult } from '../models/event-log.model';

@Injectable({ providedIn: 'root' })
export class Device {
    private readonly apiUrl = `${environment.apiUrl}/devices`;

  constructor(private readonly http: HttpClient) {}

  getAll(page = 1, pageSize = 25, search?: string): Observable<PagedResult<Devices>> {
    const params: Record<string, string> = { page: String(page), pageSize: String(pageSize) };
    if (search) params['search'] = search;
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
