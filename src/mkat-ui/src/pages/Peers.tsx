import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { peersApi } from '../api/services';
import { StateIndicator } from '../components/services/StateIndicator';

export function Peers() {
  const queryClient = useQueryClient();
  const [showPairDialog, setShowPairDialog] = useState(false);

  const { data: peers, isLoading } = useQuery({
    queryKey: ['peers'],
    queryFn: () => peersApi.list(),
  });

  const unpairMutation = useMutation({
    mutationFn: (id: string) => peersApi.unpair(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['peers'] }),
  });

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Peers</h1>
        <button
          onClick={() => setShowPairDialog(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Pair Instance
        </button>
      </div>

      {showPairDialog && (
        <PairDialog onClose={() => setShowPairDialog(false)} />
      )}

      {peers?.length === 0 ? (
        <div className="bg-white rounded-lg shadow p-6 text-center text-gray-500">
          No paired instances. Click "Pair Instance" to connect with another mkat instance.
        </div>
      ) : (
        <div className="space-y-4">
          {peers?.map(peer => (
            <div key={peer.id} className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <div className="flex items-center gap-3">
                    <h2 className="text-lg font-semibold text-gray-900">{peer.name}</h2>
                    {peer.serviceState != null && (
                      <StateIndicator state={peer.serviceState} size="sm" />
                    )}
                  </div>
                  <p className="text-sm text-gray-500 mt-1">{peer.url}</p>
                  <p className="text-xs text-gray-400 mt-1">
                    Paired {new Date(peer.pairedAt).toLocaleString()} | Heartbeat every {peer.heartbeatIntervalSeconds}s
                  </p>
                </div>
                <div className="flex gap-2">
                  <Link
                    to="/services/$serviceId"
                    params={{ serviceId: peer.serviceId }}
                    className="px-3 py-1.5 text-sm bg-blue-100 text-blue-800 rounded hover:bg-blue-200"
                  >
                    View Service
                  </Link>
                  <button
                    onClick={() => {
                      if (confirm(`Unpair from ${peer.name}?`)) {
                        unpairMutation.mutate(peer.id);
                      }
                    }}
                    className="px-3 py-1.5 text-sm bg-red-100 text-red-800 rounded hover:bg-red-200"
                  >
                    Unpair
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function PairDialog({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<'choose' | 'generate' | 'enter'>('choose');
  const [instanceName, setInstanceName] = useState('');
  const [generatedToken, setGeneratedToken] = useState('');
  const [pasteToken, setPasteToken] = useState('');

  const initiateMutation = useMutation({
    mutationFn: (name: string) => peersApi.initiate(name),
    onSuccess: (data) => {
      setGeneratedToken(data.token);
    },
  });

  const completeMutation = useMutation({
    mutationFn: (token: string) => peersApi.complete(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['peers'] });
      onClose();
    },
  });

  return (
    <div className="bg-white rounded-lg shadow p-6 border border-blue-200">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Pair with Another Instance</h2>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
          Close
        </button>
      </div>

      {mode === 'choose' && (
        <div className="space-y-3">
          <button
            onClick={() => setMode('generate')}
            className="block w-full text-left p-4 border rounded hover:bg-gray-50"
          >
            <div className="font-medium">Generate a pairing token</div>
            <div className="text-sm text-gray-500">Share the token with the other instance</div>
          </button>
          <button
            onClick={() => setMode('enter')}
            className="block w-full text-left p-4 border rounded hover:bg-gray-50"
          >
            <div className="font-medium">Enter a pairing token</div>
            <div className="text-sm text-gray-500">Paste a token from another instance</div>
          </button>
        </div>
      )}

      {mode === 'generate' && !generatedToken && (
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700">Instance Name</label>
            <input
              type="text"
              value={instanceName}
              onChange={e => setInstanceName(e.target.value)}
              placeholder="e.g. Home Server"
              className="mt-1 block w-full rounded border-gray-300 shadow-sm px-3 py-2 border"
            />
          </div>
          <button
            onClick={() => initiateMutation.mutate(instanceName)}
            disabled={!instanceName || initiateMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {initiateMutation.isPending ? 'Generating...' : 'Generate Token'}
          </button>
          {initiateMutation.isError && (
            <p className="text-red-600 text-sm">{(initiateMutation.error as Error).message}</p>
          )}
        </div>
      )}

      {mode === 'generate' && generatedToken && (
        <div className="space-y-3">
          <p className="text-sm text-gray-600">
            Share this token with the other instance. It expires in 10 minutes.
          </p>
          <div className="relative">
            <textarea
              readOnly
              value={generatedToken}
              className="block w-full rounded border-gray-300 shadow-sm px-3 py-2 border text-xs font-mono"
              rows={3}
            />
            <button
              onClick={() => navigator.clipboard.writeText(generatedToken)}
              className="absolute top-2 right-2 px-2 py-1 text-xs bg-gray-100 rounded hover:bg-gray-200"
            >
              Copy
            </button>
          </div>
        </div>
      )}

      {mode === 'enter' && (
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700">Pairing Token</label>
            <textarea
              value={pasteToken}
              onChange={e => setPasteToken(e.target.value)}
              placeholder="Paste the token from the other instance"
              className="mt-1 block w-full rounded border-gray-300 shadow-sm px-3 py-2 border text-xs font-mono"
              rows={3}
            />
          </div>
          <button
            onClick={() => completeMutation.mutate(pasteToken.trim())}
            disabled={!pasteToken.trim() || completeMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {completeMutation.isPending ? 'Pairing...' : 'Complete Pairing'}
          </button>
          {completeMutation.isError && (
            <p className="text-red-600 text-sm">{(completeMutation.error as Error).message}</p>
          )}
        </div>
      )}
    </div>
  );
}
