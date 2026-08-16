import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { ResourceSample } from '../../../core/models/device.model';
@Component({ selector: 'app-resource-history', imports: [CommonModule, DatePipe, RouterLink], templateUrl: './resource-history.html', styleUrl: './resource-history.css' })
export class ResourceHistory implements OnInit { private readonly monitoring = inject(Monitoring); private readonly route = inject(ActivatedRoute); readonly points = signal<ResourceSample[]>([]); readonly error = signal<string | null>(null); ngOnInit(): void { const id = this.route.snapshot.paramMap.get('id'); if (id) this.monitoring.getResourceHistory(id).subscribe({ next: points => this.points.set(points), error: () => this.error.set('Unable to load resource history.') }); } }
