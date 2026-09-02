import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-resource-usage-chart',
  imports: [],
  templateUrl: './resource-usage-chart.html',
  styleUrl: './resource-usage-chart.css',
})
export class ResourceUsageChart {
  readonly timeRanges = ['1H', '6H', '24H', '7D'];
  readonly selectedRange = signal('24H');
  setRange(range: string) { this.selectedRange.set(range); }
  selectRange(range: string) { this.setRange(range); }
}
