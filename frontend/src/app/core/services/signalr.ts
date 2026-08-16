import { Injectable } from '@angular/core';
import { DeviceStatusUpdate } from '../models/ping-result.model';
import { Subject } from 'rxjs';
import { ResourceSample } from '../models/device.model';
import { EventLogEntry } from '../models/event-log.model';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import * as signalR from '@microsoft/signalr';


const HUB_PATH = '/hubs/monitoring';

@Injectable({ providedIn: 'root' })
export class Signalr {
  private hubConnection?: signalR.HubConnection;

  private readonly deviceStatusSubject = new Subject<DeviceStatusUpdate>();
  private readonly resourceSampleSubject = new Subject<ResourceSample>();
  private readonly eventLogSubject = new Subject<EventLogEntry>();

  readonly deviceStatus$ = this.deviceStatusSubject.asObservable();
  readonly resourceSample$ = this.resourceSampleSubject.asObservable();
  readonly eventLog$ = this.eventLogSubject.asObservable();

  constructor(private readonly authService: AuthService) {}

  /** Call once after login (e.g. in the admin layout's constructor/ngOnInit). */
  connect(): void {
    if (this.hubConnection) return;

    const hubUrl = this.resolveHubUrl();

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on('DeviceStatusChanged', (payload: DeviceStatusUpdate) =>
      this.deviceStatusSubject.next(payload),
    );
    this.hubConnection.on('ResourceSampleReceived', (payload: ResourceSample) =>
      this.resourceSampleSubject.next(payload),
    );
    this.hubConnection.on('EventLogged', (payload: EventLogEntry) =>
      this.eventLogSubject.next(payload),
    );

    this.hubConnection.start().catch((err) => console.error('SignalR connection failed:', err));
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  private resolveHubUrl(): string {
    // Strip a trailing /api so we land on the host root + /hubs/monitoring.
    const base = environment.apiUrl.replace(/\/api\/?$/, '');
    return `${base}${HUB_PATH}`;
  }
}
