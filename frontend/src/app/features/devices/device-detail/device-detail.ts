import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Device } from '../../../core/services/device';
import { Devices as DeviceModel } from '../../../core/models/device.model';
@Component({ selector: 'app-device-detail', imports: [CommonModule, RouterLink, DatePipe], templateUrl: './device-detail.html' })
export class DeviceDetail implements OnInit { private readonly service = inject(Device); private readonly route = inject(ActivatedRoute); readonly device = signal<DeviceModel | null>(null); readonly error = signal<string | null>(null); ngOnInit(): void { const id = this.route.snapshot.paramMap.get('id'); if (id) this.service.getById(id).subscribe({ next: d => this.device.set(d), error: () => this.error.set('Device not found.') }); } }
