import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Alert } from '../../../core/services/alert';
import { AlertRule } from '../../../core/models/alert.model';
@Component({ selector: 'app-alert-rules', imports: [CommonModule, RouterLink], templateUrl: './alert-rules.html', styleUrl: './alert-rules.css' })
export class AlertRules implements OnInit { private readonly alerts = inject(Alert); readonly rules = signal<AlertRule[]>([]); readonly error = signal<string | null>(null); ngOnInit(): void { this.load(); } load(): void { this.alerts.getRules().subscribe({ next: rules => this.rules.set(rules), error: () => this.error.set('Unable to load alert rules.') }); } delete(id: string): void { this.alerts.deleteRule(id).subscribe({ next: () => this.load(), error: () => this.error.set('Unable to delete rule.') }); } }
