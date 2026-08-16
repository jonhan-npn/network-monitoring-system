import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Alert } from '../../../core/services/alert';
import { AlertHistoryEntry } from '../../../core/models/alert.model';
@Component({ selector: 'app-alert-history', imports: [CommonModule, RouterLink, DatePipe], templateUrl: './alert-history.html', styleUrl: './alert-history.css' })
export class AlertHistory implements OnInit { private readonly alerts = inject(Alert); readonly history = signal<AlertHistoryEntry[]>([]); readonly error = signal<string | null>(null); ngOnInit(): void { this.alerts.getHistory().subscribe({ next: result => this.history.set(result.items), error: () => this.error.set('Unable to load alert history.') }); } }
