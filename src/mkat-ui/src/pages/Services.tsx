import { useState } from 'react';
import { Link } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { ServiceCard } from '../components/services/ServiceCard';
import { Pagination } from '../components/common/Pagination';
import { buttonVariants } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Info } from 'lucide-react';

export function Services() {
  const [page, setPage] = useState(1);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['services', page],
    queryFn: () => servicesApi.list(page, 20),
  });

  const pauseMutation = useMutation({
    mutationFn: (id: string) => servicesApi.pause(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  const resumeMutation = useMutation({
    mutationFn: (id: string) => servicesApi.resume(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['services'] }),
  });

  if (isLoading) return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-32" />
        <Skeleton className="h-9 w-28 rounded-md" />
      </div>
      <div className="space-y-4">
        <Skeleton className="h-24 w-full rounded-lg" />
        <Skeleton className="h-24 w-full rounded-lg" />
        <Skeleton className="h-24 w-full rounded-lg" />
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">Services</h1>
        <Link
          to="/services/new"
          className={buttonVariants()}
        >
          Add Service
        </Link>
      </div>

      {data?.items.length === 0 ? (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription>
            No services configured yet.{' '}
            <Link to="/services/new" className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 underline">
              Create your first service
            </Link>
          </AlertDescription>
        </Alert>
      ) : (
        <div className="space-y-4">
          {data?.items.map(service => (
            <ServiceCard
              key={service.id}
              service={service}
              onPause={() => pauseMutation.mutate(service.id)}
              onResume={() => resumeMutation.mutate(service.id)}
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
