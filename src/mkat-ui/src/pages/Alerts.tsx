import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { alertsApi } from '../api/alerts';
import { AlertItem } from '../components/alerts/AlertItem';
import { Pagination } from '../components/common/Pagination';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Info } from 'lucide-react';

export function Alerts() {
  const [page, setPage] = useState(1);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['alerts', page],
    queryFn: () => alertsApi.list(page, 20),
    refetchInterval: 30000,
  });

  const ackMutation = useMutation({
    mutationFn: (id: string) => alertsApi.acknowledge(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  if (isLoading) return (
    <div className="space-y-6">
      <Skeleton className="h-8 w-24" />
      <div className="space-y-3">
        <Skeleton className="h-16 w-full rounded-lg" />
        <Skeleton className="h-16 w-full rounded-lg" />
        <Skeleton className="h-16 w-full rounded-lg" />
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-foreground">Alerts</h1>

      {data?.items.length === 0 ? (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription>No alerts yet.</AlertDescription>
        </Alert>
      ) : (
        <div className="space-y-3">
          {data?.items.map(alert => (
            <AlertItem
              key={alert.id}
              alert={alert}
              onAcknowledge={() => ackMutation.mutate(alert.id)}
            />
          ))}
        </div>
      )}

      {data && (
        <Pagination
          page={page}
          totalCount={data.totalCount}
          pageSize={20}
          onPageChange={setPage}
        />
      )}
    </div>
  );
}
