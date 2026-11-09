export interface RangeOption {
  label: string;
  hours: number;
}

export const HISTORY_RANGES: RangeOption[] = [
  { label: '1H', hours: 1 },
  { label: '6H', hours: 6 },
  { label: '24H', hours: 24 },
  { label: '7D', hours: 24 * 7 },
];

export function rangeWindow(hours: number, now = new Date()): { from: Date; to: Date } {
  return { from: new Date(now.getTime() - hours * 60 * 60 * 1000), to: now };
}

/** Groups values into at most `maxPoints` buckets, averaging the samples in each. */
export function bucketAverages<T>(
  items: T[],
  maxPoints: number,
  valueOf: (item: T) => number | null | undefined,
  labelOf: (item: T) => string,
): { labels: string[]; values: Array<number | null> } {
  if (!items.length) return { labels: [], values: [] };

  const bucketCount = Math.min(maxPoints, items.length);
  const size = Math.ceil(items.length / bucketCount);
  const labels: string[] = [];
  const values: Array<number | null> = [];

  for (let start = 0; start < items.length; start += size) {
    const bucket = items.slice(start, start + size);
    const numbers = bucket
      .map(valueOf)
      .filter((value): value is number => value != null && isFinite(value));
    labels.push(labelOf(bucket[0]));
    values.push(numbers.length ? numbers.reduce((sum, value) => sum + value, 0) / numbers.length : null);
  }

  return { labels, values };
}
