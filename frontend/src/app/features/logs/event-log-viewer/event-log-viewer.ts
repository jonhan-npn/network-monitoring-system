import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { EventLogEntry } from '../../../core/models/event-log.model';

@Component({ selector: 'app-event-log-viewer', imports: [CommonModule, FormsModule, RouterLink, DatePipe], templateUrl: './event-log-viewer.html', styleUrl: './event-log-viewer.css' })
export class EventLogViewer implements OnInit {
  private readonly monitoring = inject(Monitoring);
  readonly query = signal(''); readonly events = signal<EventLogEntry[]>([]); readonly totalCount = signal(0); readonly loading = signal(true); readonly error = signal<string | null>(null);
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.monitoring.getEvents({ page: 1, pageSize: 100 }).subscribe({ next: result => { const search = this.query().toLowerCase(); this.events.set(result.items.filter(item => !search || `${item.eventType} ${item.message} ${item.deviceName ?? ''}`.toLowerCase().includes(search))); this.totalCount.set(result.totalCount); this.loading.set(false); }, error: () => { this.error.set('Unable to load event log.'); this.loading.set(false); } }); }
}
