export type TimeRange = '1h' | '6h' | '24h' | '7d' | '30d' | '90d' | '1y';

export function getTimeRangeDate(range: TimeRange): Date {
  const now = new Date();
  switch (range) {
    case '1h':
      return new Date(now.getTime() - 60 * 60 * 1000);
    case '6h':
      return new Date(now.getTime() - 6 * 60 * 60 * 1000);
    case '24h':
      return new Date(now.getTime() - 24 * 60 * 60 * 1000);
    case '7d':
      return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    case '30d':
      return new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    case '90d':
      return new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
    case '1y':
      return new Date(now.getTime() - 365 * 24 * 60 * 60 * 1000);
  }
}

export function getDataSource(range: TimeRange): 'events' | 'hourly' | 'daily' {
  switch (range) {
    case '1h':
    case '6h':
    case '24h':
      return 'events';
    case '7d':
    case '30d':
      return 'hourly';
    case '90d':
    case '1y':
      return 'daily';
  }
}
