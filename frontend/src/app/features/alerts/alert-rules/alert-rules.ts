import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Alert } from '../../../core/services/alert';
import { Device } from '../../../core/services/device';
import {
  ALERT_CHANNEL_LABELS,
  ALERT_METRIC_LABELS,
  AlertChannel,
  AlertMetric,
  AlertRule,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest,
} from '../../../core/models/alert.model';
import { Devices } from '../../../core/models/device.model';

type RuleControl = 'name' | 'deviceId' | 'metric' | 'thresholdPercent' | 'consecutiveBreaches' | 'channel' | 'destination' | 'isEnabled';

@Component({
  selector: 'app-alert-rules',
  imports: [CommonModule, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './alert-rules.html',
  styleUrl: './alert-rules.css',
})
export class AlertRules implements OnInit {
  private readonly alerts = inject(Alert);
  private readonly devicesApi = inject(Device);
  private readonly fb = inject(FormBuilder);

  readonly rules = signal<AlertRule[]>([]);
  readonly devices = signal<Devices[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);

  readonly channels = Object.values(AlertChannel);
  readonly metrics = Object.values(AlertMetric);
  readonly channelLabels = ALERT_CHANNEL_LABELS;
  readonly metricLabels = ALERT_METRIC_LABELS;

  readonly modalOpen = signal(false);
  readonly editing = signal<AlertRule | null>(null);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly pendingDelete = signal<AlertRule | null>(null);
  readonly deleting = signal(false);
  readonly togglingId = signal<string | null>(null);

  readonly testOpen = signal(false);
  readonly testing = signal(false);
  readonly testError = signal<string | null>(null);
  readonly testSuccess = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    deviceId: [''],
    metric: [AlertMetric.Cpu as string, Validators.required],
    thresholdPercent: [80, [Validators.min(1), Validators.max(100)]],
    consecutiveBreaches: [3, [Validators.required, Validators.min(1), Validators.max(50)]],
    channel: [AlertChannel.Email as string, Validators.required],
    destination: ['', [Validators.required]],
    isEnabled: [true],
  });

  readonly testForm = this.fb.nonNullable.group({
    channel: [AlertChannel.Email as string, Validators.required],
    destination: ['', [Validators.required]],
  });

  readonly enabledCount = () => this.rules().filter((rule) => rule.isEnabled).length;

  ngOnInit(): void {
    this.load();

    this.devicesApi.getAll({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.devices.set(result.items),
      error: () => console.warn('Could not load devices for alert targeting.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.alerts.getRules().subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load alert rules.');
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.formError.set(null);
    this.form.reset({
      name: '',
      deviceId: '',
      metric: AlertMetric.Cpu,
      thresholdPercent: 80,
      consecutiveBreaches: 3,
      channel: AlertChannel.Email,
      destination: '',
      isEnabled: true,
    });
    this.modalOpen.set(true);
  }

  openEdit(rule: AlertRule): void {
    this.editing.set(rule);
    this.formError.set(null);
    this.form.reset({
      name: rule.name,
      deviceId: rule.deviceId ?? '',
      metric: rule.metric,
      thresholdPercent: rule.thresholdPercent ?? 80,
      consecutiveBreaches: rule.consecutiveBreaches,
      channel: rule.channel,
      destination: rule.destination,
      isEnabled: rule.isEnabled,
    });
    this.modalOpen.set(true);
  }

  closeModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
  }

  isAvailability(): boolean {
    return this.form.controls.metric.value === AlertMetric.Availability;
  }

  invalid(name: RuleControl): boolean {
    const control = this.form.controls[name];
    return control.invalid && control.touched;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const availability = raw.metric === AlertMetric.Availability;

    if (!availability && (raw.thresholdPercent == null || raw.thresholdPercent <= 0)) {
      this.formError.set('Enter a threshold between 1 and 100 percent.');
      return;
    }

    const request: CreateAlertRuleRequest = {
      name: raw.name.trim(),
      deviceId: raw.deviceId || null,
      metric: raw.metric,
      thresholdPercent: availability ? null : Number(raw.thresholdPercent),
      consecutiveBreaches: Number(raw.consecutiveBreaches),
      channel: raw.channel,
      destination: raw.destination.trim(),
    };

    this.saving.set(true);
    this.formError.set(null);

    const editing = this.editing();
    const result = editing
      ? this.alerts.updateRule(editing.id, { ...request, isEnabled: raw.isEnabled } as UpdateAlertRuleRequest)
      : this.alerts.createRule(request);

    result.subscribe({
      next: () => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.flash(editing ? 'Alert rule updated.' : 'Alert rule created.');
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err?.friendlyMessage ?? 'Unable to save this alert rule.');
      },
    });
  }

  toggleEnabled(rule: AlertRule): void {
    this.actionError.set(null);
    this.togglingId.set(rule.id);

    const request: UpdateAlertRuleRequest = {
      name: rule.name,
      deviceId: rule.deviceId ?? null,
      metric: rule.metric,
      thresholdPercent: rule.thresholdPercent ?? null,
      consecutiveBreaches: rule.consecutiveBreaches,
      channel: rule.channel,
      destination: rule.destination,
      isEnabled: !rule.isEnabled,
    };

    this.alerts.updateRule(rule.id, request).subscribe({
      next: (updated) => {
        this.rules.update((rules) => rules.map((item) => (item.id === rule.id ? updated : item)));
        this.togglingId.set(null);
      },
      error: () => {
        this.togglingId.set(null);
        this.actionError.set('Unable to change this rule. Operators and admins can edit rules.');
      },
    });
  }

  confirmDelete(): void {
    const rule = this.pendingDelete();
    if (!rule) return;

    this.deleting.set(true);
    this.alerts.deleteRule(rule.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.flash('Alert rule deleted.');
        this.load();
      },
      error: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.actionError.set('Unable to delete this rule. Only admins can delete alert rules.');
      },
    });
  }

  openTest(): void {
    this.testError.set(null);
    this.testSuccess.set(null);
    this.testOpen.set(true);
  }

  sendTest(): void {
    if (this.testForm.invalid) {
      this.testForm.markAllAsTouched();
      return;
    }

    this.testing.set(true);
    this.testError.set(null);
    this.testSuccess.set(null);

    const { channel, destination } = this.testForm.getRawValue();

    this.alerts.sendTest({ channel, destination: destination.trim() }).subscribe({
      next: () => {
        this.testing.set(false);
        this.testSuccess.set('Test alert accepted. Check the destination inbox / chat.');
      },
      error: (err) => {
        this.testing.set(false);
        this.testError.set(
          err?.status === 502
            ? 'Delivery failed. Check the channel configuration and the Worker logs.'
            : err?.friendlyMessage ?? 'Unable to send the test alert.',
        );
      },
    });
  }

  metricLabel(rule: AlertRule): string {
    if (rule.metric === AlertMetric.Availability) return 'Device offline';
    const base = rule.metric === AlertMetric.Cpu ? 'CPU usage' : 'Memory usage';
    return rule.thresholdPercent != null ? `${base} > ${rule.thresholdPercent}%` : base;
  }

  deviceLabel(rule: AlertRule): string {
    return rule.deviceName ?? 'All devices';
  }

  badgeForChannel(channel: string): string {
    return channel === AlertChannel.Telegram ? 'badge-info' : 'badge-brand';
  }

  private flash(message: string): void {
    this.actionMessage.set(message);
    setTimeout(() => this.actionMessage.set(null), 3200);
  }
}
