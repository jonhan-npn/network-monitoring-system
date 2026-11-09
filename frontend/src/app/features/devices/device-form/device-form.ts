import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Device } from '../../../core/services/device';
import {
  CreateDeviceRequest,
  DEVICE_TYPE_LABELS,
  DeviceType,
  SNMP_VERSIONS,
} from '../../../core/models/device.model';

type DeviceControl =
  | 'name'
  | 'ipAddress'
  | 'deviceType'
  | 'location'
  | 'description'
  | 'snmpCommunity'
  | 'snmpPort'
  | 'snmpVersion'
  | 'sshPort'
  | 'sshUsername'
  | 'sshCredentialRef';

@Component({
  selector: 'app-device-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './device-form.html',
  styleUrl: './device-form.css',
})
export class DeviceForm implements OnInit {
  private readonly service = inject(Device);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly id = this.route.snapshot.paramMap.get('id');
  readonly isEdit = !!this.id;
  readonly loading = signal(this.isEdit);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly types = Object.values(DeviceType);
  readonly typeLabels = DEVICE_TYPE_LABELS;
  readonly snmpVersions = SNMP_VERSIONS;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    ipAddress: [
      '',
      [
        Validators.required,
        Validators.pattern(
          /^(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)$|^[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/,
        ),
      ],
    ],
    deviceType: [DeviceType.Other, Validators.required],
    location: [''],
    description: [''],
    snmpCommunity: ['public'],
    snmpPort: [161, [Validators.min(1), Validators.max(65535)]],
    snmpVersion: ['2c'],
    sshPort: [22, [Validators.min(1), Validators.max(65535)]],
    sshUsername: [''],
    sshCredentialRef: [''],
  });

  ngOnInit(): void {
    if (!this.id) return;

    this.service.getById(this.id).subscribe({
      next: (device) => {
        this.form.patchValue({
          name: device.name,
          ipAddress: device.ipAddress,
          deviceType: device.deviceType,
          location: device.location ?? '',
          description: device.description ?? '',
          snmpCommunity: device.snmpCommunity ?? 'public',
          snmpPort: device.snmpPort ?? 161,
          snmpVersion: device.snmpVersion ?? '2c',
          sshPort: device.sshPort ?? 22,
          sshUsername: device.sshUsername ?? '',
          sshCredentialRef: device.sshCredentialRef ?? '',
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load this device.');
        this.loading.set(false);
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const raw = this.form.getRawValue();
    const request: CreateDeviceRequest = {
      name: raw.name.trim(),
      ipAddress: raw.ipAddress.trim(),
      deviceType: raw.deviceType as DeviceType,
      location: raw.location.trim() || null,
      description: raw.description.trim() || null,
      snmpCommunity: raw.snmpCommunity.trim() || 'public',
      snmpPort: Number(raw.snmpPort),
      snmpVersion: raw.snmpVersion,
      sshPort: Number(raw.sshPort),
      sshUsername: raw.sshUsername.trim() || null,
      sshCredentialRef: raw.sshCredentialRef.trim() || null,
    };

    const result = this.id ? this.service.update(this.id, request) : this.service.create(request);

    result.subscribe({
      next: (device) => this.router.navigate(['/devices', device.id]),
      error: (err) => {
        this.error.set(err?.friendlyMessage ?? 'Unable to save this device. Please check the fields and try again.');
        this.saving.set(false);
      },
    });
  }

  invalid(name: DeviceControl): boolean {
    const control = this.form.controls[name];
    return control.invalid && control.touched;
  }
}
