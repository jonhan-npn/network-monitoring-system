import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { PingResult } from '../../../core/models/ping-result.model';
@Component({ selector: 'app-ping-history', imports: [CommonModule, DatePipe, RouterLink], templateUrl: './ping-history.html', styleUrl: './ping-history.css' })
export class PingHistory implements OnInit { private readonly monitoring = inject(Monitoring); private readonly route = inject(ActivatedRoute); readonly points = signal<PingResult[]>([]); readonly error = signal<string | null>(null); ngOnInit(): void { const id = this.route.snapshot.paramMap.get('id'); if (id) this.monitoring.getPingHistory(id).subscribe({ next: points => this.points.set(points), error: () => this.error.set('Unable to load ping history.') }); } }
