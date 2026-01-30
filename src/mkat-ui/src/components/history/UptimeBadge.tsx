import { Badge } from '@/components/ui/badge';

interface UptimeBadgeProps {
  percent: number | null | undefined;
}

export function UptimeBadge({ percent }: UptimeBadgeProps) {
  if (percent == null) {
    return (
      <Badge variant="secondary" className="text-xs font-mono">
        --% uptime
      </Badge>
    );
  }

  let variant: 'default' | 'secondary' | 'destructive' = 'default';
  let colorClasses = 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200';

  if (percent < 95) {
    variant = 'destructive';
    colorClasses = '';
  } else if (percent < 99) {
    variant = 'secondary';
    colorClasses = 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200';
  }

  return (
    <Badge
      variant={variant}
      className={`text-xs font-mono ${variant !== 'destructive' ? colorClasses : ''}`}
    >
      {percent.toFixed(2)}% uptime
    </Badge>
  );
}
