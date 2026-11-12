export type Granularity = 'day' | 'week';

export interface SeriesBucket<T extends { date: string }> {
  date: string;
  points: T[];
  last: T;
}

const MILLISECONDS_PER_DAY = 86_400_000;
const DAYS_PER_WEEK = 7;
const DAYS_FROM_MONDAY_TO_EPOCH_DAY = 3;

export function bucketsOf<T extends { date: string }>(
  series: T[],
  granularity: Granularity,
): SeriesBucket<T>[] {
  const buckets: SeriesBucket<T>[] = [];
  let currentKey: number | null = null;

  for (const point of series) {
    const key = bucketKeyOf(point.date, granularity);

    if (key !== currentKey) buckets.push({ date: point.date, points: [], last: point });

    const bucket = buckets[buckets.length - 1];

    bucket.points.push(point);
    bucket.date = point.date;
    bucket.last = point;
    currentKey = key;
  }

  return buckets;
}

export function bucketDatesByDate<T extends { date: string }>(
  buckets: SeriesBucket<T>[],
): Map<string, string> {
  return new Map(buckets.flatMap((bucket) => bucket.points.map((point) => [point.date, bucket.date])));
}

function bucketKeyOf(isoDate: string, granularity: Granularity): number {
  const days = Math.floor(Date.parse(isoDate) / MILLISECONDS_PER_DAY);

  return granularity === 'day'
    ? days
    : Math.floor((days + DAYS_FROM_MONDAY_TO_EPOCH_DAY) / DAYS_PER_WEEK);
}
