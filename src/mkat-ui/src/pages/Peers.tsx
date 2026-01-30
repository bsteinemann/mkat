import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { toast } from 'sonner';
import { peersApi } from '../api/services';
import { StateIndicator } from '../components/services/StateIndicator';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent } from '@/components/ui/card';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';

export function Peers() {
  const queryClient = useQueryClient();
  const [showPairDialog, setShowPairDialog] = useState(false);

  const { data: peers, isLoading } = useQuery({
    queryKey: ['peers'],
    queryFn: () => peersApi.list(),
  });

  const unpairMutation = useMutation({
    mutationFn: (id: string) => peersApi.unpair(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['peers'] });
      toast.success('Peer unpaired');
    },
    onError: () => {
      toast.error('Failed to unpair peer');
    },
  });

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Peers</h1>
        <Button onClick={() => setShowPairDialog(true)}>
          Pair Instance
        </Button>
      </div>

      {showPairDialog && (
        <PairDialog onClose={() => setShowPairDialog(false)} />
      )}

      {peers?.length === 0 ? (
        <Card className="py-0">
          <CardContent className="p-6 text-center text-gray-500">
            No paired instances. Click "Pair Instance" to connect with another mkat instance.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {peers?.map(peer => (
            <Card key={peer.id} className="py-0">
              <CardContent className="p-6">
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
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button variant="destructive" size="sm">Unpair</Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Unpair from {peer.name}?</AlertDialogTitle>
                          <AlertDialogDescription>
                            This will disconnect the peer and stop monitoring its heartbeat.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>
                          <AlertDialogAction onClick={() => unpairMutation.mutate(peer.id)}>
                            Unpair
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </div>
                </div>
              </CardContent>
            </Card>
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
    onError: () => {
      toast.error('Failed to generate pairing token');
    },
  });

  const completeMutation = useMutation({
    mutationFn: (token: string) => peersApi.complete(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['peers'] });
      toast.success('Pairing complete');
      onClose();
    },
    onError: () => {
      toast.error('Failed to complete pairing');
    },
  });

  return (
    <Card className="border-blue-200 py-0">
      <CardContent className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Pair with Another Instance</h2>
        <Button variant="ghost" size="sm" onClick={onClose}>
          Close
        </Button>
      </div>

      {mode === 'choose' && (
        <div className="space-y-3">
          <Button
            variant="outline"
            className="block w-full text-left p-4 h-auto"
            onClick={() => setMode('generate')}
          >
            <div>
              <div className="font-medium">Generate a pairing token</div>
              <div className="text-sm text-gray-500">Share the token with the other instance</div>
            </div>
          </Button>
          <Button
            variant="outline"
            className="block w-full text-left p-4 h-auto"
            onClick={() => setMode('enter')}
          >
            <div>
              <div className="font-medium">Enter a pairing token</div>
              <div className="text-sm text-gray-500">Paste a token from another instance</div>
            </div>
          </Button>
        </div>
      )}

      {mode === 'generate' && !generatedToken && (
        <div className="space-y-3">
          <div className="space-y-2">
            <Label>Instance Name</Label>
            <Input
              type="text"
              value={instanceName}
              onChange={e => setInstanceName(e.target.value)}
              placeholder="e.g. Home Server"
            />
          </div>
          <Button
            onClick={() => initiateMutation.mutate(instanceName)}
            disabled={!instanceName || initiateMutation.isPending}
          >
            {initiateMutation.isPending ? 'Generating...' : 'Generate Token'}
          </Button>
        </div>
      )}

      {mode === 'generate' && generatedToken && (
        <div className="space-y-3">
          <p className="text-sm text-gray-600">
            Share this token with the other instance. It expires in 10 minutes.
          </p>
          <div className="relative">
            <Textarea
              readOnly
              value={generatedToken}
              className="text-xs font-mono"
              rows={3}
            />
            <Button
              variant="secondary"
              size="xs"
              className="absolute top-2 right-2"
              onClick={() => navigator.clipboard.writeText(generatedToken)}
            >
              Copy
            </Button>
          </div>
        </div>
      )}

      {mode === 'enter' && (
        <div className="space-y-3">
          <div className="space-y-2">
            <Label>Pairing Token</Label>
            <Textarea
              value={pasteToken}
              onChange={e => setPasteToken(e.target.value)}
              placeholder="Paste the token from the other instance"
              className="text-xs font-mono"
              rows={3}
            />
          </div>
          <Button
            onClick={() => completeMutation.mutate(pasteToken.trim())}
            disabled={!pasteToken.trim() || completeMutation.isPending}
          >
            {completeMutation.isPending ? 'Pairing...' : 'Complete Pairing'}
          </Button>
        </div>
      )}
      </CardContent>
    </Card>
  );
}
