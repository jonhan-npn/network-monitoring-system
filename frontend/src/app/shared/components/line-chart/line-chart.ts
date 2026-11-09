import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';

export interface ChartSeries {
  name: string;
  color: string;
  values: Array<number | null>;
}

interface SeriesGeometry {
  name: string;
  color: string;
  lines: string[];
  areas: string[];
}

interface GridLine {
  y: number;
  label: string;
}

interface XTick {
  x: number;
  label: string;
}

interface ChartView {
  series: SeriesGeometry[];
  gridY: GridLine[];
  xTicks: XTick[];
}

@Component({
  selector: 'app-line-chart',
  imports: [CommonModule],
  templateUrl: './line-chart.html',
  styleUrl: './line-chart.css',
})
export class LineChart {
  readonly series = input<ChartSeries[]>([]);
  readonly labels = input<string[]>([]);
  readonly ySuffix = input('');
  readonly yMax = input<number | null>(null);
  readonly emptyLabel = input('No samples recorded for this period yet.');
  readonly height = input(250);
  readonly width = 800;

  private readonly pad = { top: 16, right: 16, bottom: 28, left: 46 };

  readonly hasData = computed(() =>
    this.series().some((item) => item.values.some((value) => value != null && isFinite(value))),
  );

  readonly view = computed<ChartView>(() => {
    const series = this.series();
    const labels = this.labels();
    const count = Math.max(labels.length, ...series.map((item) => item.values.length), 0);
    const allValues = series.flatMap((item) => item.values).filter((value): value is number => value != null);
    const max = this.yMax() ?? niceMax(allValues.length ? Math.max(...allValues) : 0);
    const innerWidth = this.width - this.pad.left - this.pad.right;
    const innerHeight = this.height() - this.pad.top - this.pad.bottom;
    const baseY = this.pad.top + innerHeight;

    const xAt = (index: number) =>
      count <= 1 ? this.pad.left + innerWidth / 2 : this.pad.left + (index / (count - 1)) * innerWidth;
    const yAt = (value: number) => this.pad.top + innerHeight * (1 - value / max);

    const gridY: GridLine[] = [];
    for (let step = 0; step <= 4; step += 1) {
      const value = (max / 4) * step;
      gridY.push({ y: yAt(value), label: `${formatNumber(value)}${this.ySuffix()}` });
    }

    const xTicks: XTick[] = [];
    if (count > 0) {
      const stride = Math.max(1, Math.ceil(count / 6));
      for (let index = 0; index < count; index += stride) {
        xTicks.push({ x: xAt(index), label: labels[index] ?? '' });
      }
      const last = count - 1;
      if (xTicks[xTicks.length - 1]?.label !== (labels[last] ?? '') && labels[last]) {
        xTicks.push({ x: xAt(last), label: labels[last] });
      }
    }

    const geometry: SeriesGeometry[] = series.map((item) => {
      const lines: string[] = [];
      const areas: string[] = [];
      let current: string[] = [];

      const flush = () => {
        if (current.length > 1) {
          const line = current.join(' ');
          lines.push(line);
          const first = current[0].replace('M', '').trim().split(' ');
          const lastPoint = current[current.length - 1].replace('L', '').trim().split(' ');
          areas.push(
            `${line} L ${lastPoint[0]} ${baseY} L ${first[0]} ${baseY} Z`,
          );
        } else if (current.length === 1) {
          lines.push(current[0]);
        }
        current = [];
      };

      item.values.slice(0, count).forEach((value, index) => {
        if (value == null || !isFinite(value)) {
          flush();
          return;
        }
        const point = `${xAt(index).toFixed(2)} ${yAt(value).toFixed(2)}`;
        current.push(current.length ? `L ${point}` : `M ${point}`);
      });
      flush();

      return { name: item.name, color: item.color, lines, areas };
    });

    return { series: geometry, gridY, xTicks };
  });

  protected readonly trackByIndex = (index: number) => index;
}

function niceMax(value: number): number {
  if (!isFinite(value) || value <= 0) return 10;
  const magnitude = Math.pow(10, Math.floor(Math.log10(value)));
  const normalized = value / magnitude;
  const step = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
  return step * magnitude;
}

function formatNumber(value: number): string {
  if (value >= 10000) return `${Math.round(value / 1000)}k`;
  if (value >= 1000) return `${(value / 1000).toFixed(1)}k`;
  if (Number.isInteger(value)) return String(value);
  return value.toFixed(1);
}
