import { useNavigate, useParams } from '@tanstack/react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi } from '../api/services';
import { ServiceForm } from '../components/services/ServiceForm';
import type { CreateServiceRequest, UpdateServiceRequest } from '../api/types';

export function ServiceEdit() {
  const { serviceId } = useParams({ strict: false }) as { serviceId: string };
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: service, isLoading } = useQuery({
    queryKey: ['services', serviceId],
    queryFn: () => servicesApi.get(serviceId),
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateServiceRequest) => servicesApi.update(serviceId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      navigate({ to: '/services/$serviceId', params: { serviceId } });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => servicesApi.delete(serviceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      navigate({ to: '/services' });
    },
  });

  if (isLoading || !service) return <div>Loading...</div>;

  const handleSubmit = (data: CreateServiceRequest) => {
    updateMutation.mutate({
      name: data.name,
      description: data.description,
      severity: data.severity,
    });
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Edit Service</h1>
      <div className="bg-white rounded-lg shadow p-6">
        <ServiceForm
          initialData={{
            name: service.name,
            description: service.description ?? undefined,
            severity: service.severity,
          }}
          onSubmit={handleSubmit}
          isLoading={updateMutation.isPending}
          submitLabel="Update Service"
          showMonitors={false}
        />
        {updateMutation.isError && (
          <p className="text-red-600 text-sm mt-4">
            {(updateMutation.error as Error).message}
          </p>
        )}
      </div>

      <div className="mt-6 bg-white rounded-lg shadow p-6 border border-red-200">
        <h2 className="text-lg font-semibold text-red-800 mb-2">Danger Zone</h2>
        <p className="text-sm text-gray-600 mb-4">
          Deleting a service removes all monitors and alert history.
        </p>
        <button
          onClick={() => {
            if (confirm('Are you sure you want to delete this service?')) {
              deleteMutation.mutate();
            }
          }}
          className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
        >
          Delete Service
        </button>
      </div>
    </div>
  );
}
