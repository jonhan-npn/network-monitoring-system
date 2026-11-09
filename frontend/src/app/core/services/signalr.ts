import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { DeviceStatusUpdate } from '../models/ping-result.model';
import { ResourceSampleUpdate } from '../models/device.model';
import { AlertUpdate } from '../models/alert.model';
import { AuthService } from './auth.service';

const HUB_PATH = '/hubs/monitoring';
const DASHBOARD_GROUP = 'dashboard';

@Injectable({ providedIn: 'root' })
export class Signalr {
  private hubConnection?: signalR.HubConnection;

  private readonly deviceStatusSubject = new Subject<DeviceStatusUpdate>();
  private readonly resourceSampleSubject = new Subject<ResourceSampleUpdate>();
  private readonly alertSubject = new Subject<AlertUpdate>();

  readonly deviceStatus$ = this.deviceStatusSubject.asObservable();
  readonly resourceSample$ = this.resourceSampleSubject.asObservable();
  readonly alert$ = this.alertSubject.asObservable();

  constructor(private readonly authService: AuthService) {}

  /**
   * Opens the hub connection (once) and joins the dashboard broadcast group.
   * Safe to call from every page that wants live updates — reuses the
   * existing connection after the first call.
   */
  async connect(): Promise<void> {
    if (!this.hubConnection) {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(this.resolveHubUrl(), {
          accessTokenFactory: () => this.authService.getToken() ?? '',
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Method names must match IMonitoringClient in NetworkMonitor.API.Hubs.
      this.hubConnection.on('DeviceStatusChanged', (payload: DeviceStatusUpdate) =>
        this.deviceStatusSubject.next(payload),
      );
      this.hubConnection.on('ResourceSampleReceived', (payload: ResourceSampleUpdate) =>
        this.resourceSampleSubject.next(payload),
      );
      this.hubConnection.on('AlertTriggered', (payload: AlertUpdate) =>
        this.alertSubject.next(payload),
      );
    }

    if (this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
      await this.hubConnection.start();
    }

    await this.hubConnection.invoke('SubscribeToDashboard');
  }

  /** Joins a per-device group so a detail page only receives that device's updates. */
  async subscribeToDevice(deviceId: string): Promise<void> {
    await this.connect();
    await this.hubConnection!.invoke('SubscribeToDevice', deviceId);
  }

  async unsubscribeFromDevice(deviceId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('UnsubscribeFromDevice', deviceId);
    }
  }

  disconnect(): void {
    void this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  private resolveHubUrl(): string {
    // Strip a trailing /api so we land on the host root + /hubs/monitoring.
    const base = environment.apiUrl.replace(/\/api\/?$/, '');
    return `${base}${HUB_PATH}`;
  }
}
