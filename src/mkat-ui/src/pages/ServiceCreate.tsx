import { useNavigate } from '@tanstack/react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { ServiceForm } from '../components/services/ServiceForm';
import type { CreateServiceRequest } from '../api/types';

export function ServiceCreate() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (data: CreateServiceRequest) => servicesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      navigate({ to: '/services' });
    },
  });

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Create Service</h1>
      <div className="bg-white rounded-lg shadow p-6">
        <ServiceForm
          onSubmit={data => mutation.mutate(data)}
          isLoading={mutation.isPending}
          submitLabel="Create Service"
        />
        {mutation.isError && (
          <p className="text-red-600 text-sm mt-4">
            {(mutation.error as Error).message}
          </p>
        )}
      </div>
    </div>
  );
}
