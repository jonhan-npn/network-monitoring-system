import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Device } from '../../../core/services/device';
import { DeviceType, Devices as DeviceModel } from '../../../core/models/device.model';



@Component({ selector: 'app-device-list', imports: [CommonModule, FormsModule, RouterLink], templateUrl: './device-list.html' })
export class DeviceList implements OnInit {
  private readonly deviceService = inject(Device);
  readonly query = signal('');
  readonly devices = signal<DeviceModel[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  private readonly exampleDevices: DeviceModel[] = [
    { id: 'demo-router', name: 'Core Router', ipAddress: '10.10.0.1', deviceType: DeviceType.Router, location: 'HQ · Floor 1', description: 'Primary internet gateway router.', isMonitored: true, isOnline: true, lastCheckedAt: new Date().toISOString(), createdAt: new Date().toISOString() },
    { id: 'demo-switch', name: 'Warehouse Switch', ipAddress: '10.10.24.11', deviceType: DeviceType.Switch, location: 'Warehouse A', description: 'Warehouse access switch.', isMonitored: true, isOnline: true, lastCheckedAt: new Date().toISOString(), createdAt: new Date().toISOString() },
    { id: 'demo-vpn', name: 'VPN Gateway', ipAddress: '10.10.1.254', deviceType: DeviceType.Firewall, location: 'HQ · DMZ', description: 'Remote-access VPN gateway.', isMonitored: true, isOnline: false, lastCheckedAt: new Date(Date.now() - 18 * 60_000).toISOString(), createdAt: new Date().toISOString() },
    { id: 'demo-access-point', name: 'Branch Access Point', ipAddress: '10.40.2.12', deviceType: DeviceType.AccessPoint, location: 'Phnom Penh Branch', description: 'Office Wi-Fi access point.', isMonitored: false, lastCheckedAt: new Date(Date.now() - 2 * 60_000).toISOString(), createdAt: new Date().toISOString() },
    { id: 'demo-server', name: 'Database Server', ipAddress: '10.10.5.20', deviceType: DeviceType.Server, location: 'Server Room', description: 'Internal database server.', isMonitored: true, isOnline: true, lastCheckedAt: new Date().toISOString(), createdAt: new Date().toISOString() },
  ];
  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set(null);
    this.deviceService.getAll(1, 100, this.query()).subscribe({
      next: result => {
        if (result.items.length || this.query().trim()) {
          this.devices.set(result.items); this.totalCount.set(result.totalCount);
        } else {
          this.showExampleDevices();
        }
        this.loading.set(false);
      },
      error: () => { this.showExampleDevices(); this.loading.set(false); },
    });
  }
  toggleMonitoring(device: DeviceModel): void {
    if (device.id.startsWith('demo-')) {
      this.devices.update(devices => devices.map(item => item.id === device.id ? { ...item, isMonitored: !item.isMonitored } : item));
      return;
    }
    this.deviceService.setMonitoring(device.id, !device.isMonitored).subscribe({ next: () => this.load(), error: () => this.error.set('Unable to update monitoring state.') });
  }
  status(device: DeviceModel): string { return !device.isMonitored ? 'Paused' : device.isOnline === true ? 'Online' : device.isOnline === false ? 'Offline' : 'Unknown'; }
  private showExampleDevices(): void {
    const query = this.query().trim().toLowerCase();
    const matches = !query ? this.exampleDevices : this.exampleDevices.filter(device =>
      [device.name, device.ipAddress, device.location].some(value => value?.toLowerCase().includes(query)),
    );
    this.devices.set(matches);
    this.totalCount.set(matches.length);
  }



  // private readonly deviceService = inject(Device);
  // readonly query = signal('');
  // readonly devices = signal<DeviceModel[]>([]);
  // readonly totalCount = signal(0);
  // readonly loading = signal(true);
  // readonly error = signal<string | null>(null);
  // ngOnInit(): void { this.load(); }
  // load(): void {
  //   this.loading.set(true); this.error.set(null);
  //   this.deviceService.getAll(1, 100, this.query()).subscribe({
  //     next: result => { this.devices.set(result.items); this.totalCount.set(result.totalCount); this.loading.set(false); },
  //     error: () => { this.error.set('Unable to load devices.'); this.loading.set(false); },
  //   });
  // }
  // toggleMonitoring(device: DeviceModel): void {
  //   this.deviceService.setMonitoring(device.id, !device.isMonitored).subscribe({ next: () => this.load(), error: () => this.error.set('Unable to update monitoring state.') });
  // }
  // status(device: DeviceModel): string { return !device.isMonitored ? 'Paused' : device.isOnline === true ? 'Online' : device.isOnline === false ? 'Offline' : 'Unknown'; }
  
  
}
