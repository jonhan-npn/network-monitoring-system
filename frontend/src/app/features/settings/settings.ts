import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { Monitoring } from '../../core/services/monitoring';
import { Signalr } from '../../core/services/signalr';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-settings',
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  private readonly auth = inject(AuthService);
  private readonly monitoring = inject(Monitoring);
  private readonly signalr = inject(Signalr);

  readonly user = this.auth.currentUser;
  readonly apiUrl = environment.apiUrl;
  readonly authEnabled = environment.authenticationEnabled;
  readonly hubUrl = `${environment.apiUrl.replace(/\/api\/?$/, '')}/hubs/monitoring`;
  readonly year = new Date().getFullYear();

  readonly connection = signal<'idle' | 'checking' | 'ok' | 'error'>('idle');
  readonly checkedAt = signal<Date | null>(null);

  readonly initials = computed(() => {
    const username = this.user()?.username ?? '';
    const parts = username.split(/[\s._-]+/).filter(Boolean);
    return parts.length
      ? parts
          .slice(0, 2)
          .map((part) => part[0]!.toUpperCase())
          .join('')
      : 'NW';
  });

  readonly role = computed(() => this.user()?.role ?? 'Viewer');

  readonly serverConfig = [
    { label: 'Ping interval', value: '30 seconds' },
    { label: 'SNMP poll interval', value: '60 seconds' },
    { label: 'Ping timeout', value: '2 000 ms' },
    { label: 'Offline threshold', value: '2 failed checks' },
    { label: 'History retention', value: '30 days' },
    { label: 'Live updates', value: 'SignalR · /hubs/monitoring' },
  ];

  checkConnection(): void {
    this.connection.set('checking');
    this.monitoring.getSummary().subscribe({
      next: () => {
        this.connection.set('ok');
        this.checkedAt.set(new Date());
      },
      error: () => {
        this.connection.set('error');
        this.checkedAt.set(new Date());
      },
    });
  }

  testLiveUpdates(): void {
    this.signalr
      .connect()
      .then(() => {
        this.connection.set('ok');
        this.checkedAt.set(new Date());
      })
      .catch(() => {
        this.connection.set('error');
        this.checkedAt.set(new Date());
      });
  }

  logout(): void {
    this.auth.logout();
  }
}
