import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { getErrorMessage } from '../api/client';
import { contactsApi } from '../api/services';
import { ChannelType } from '../api/types';
import type { Contact, ContactChannel } from '../api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Info, MoreHorizontal } from 'lucide-react';
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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

export function Contacts() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editingContact, setEditingContact] = useState<Contact | null>(null);

  const { data: contacts, isLoading } = useQuery({
    queryKey: ['contacts'],
    queryFn: () => contactsApi.list(),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => contactsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Contact deleted');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to delete contact'));
    },
  });

  if (isLoading) return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-32" />
        <Skeleton className="h-9 w-28 rounded-md" />
      </div>
      <div className="space-y-3">
        <Skeleton className="h-20 w-full rounded-lg" />
        <Skeleton className="h-20 w-full rounded-lg" />
        <Skeleton className="h-20 w-full rounded-lg" />
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">Contacts</h1>
        <Button onClick={() => setShowCreate(true)}>
          Add Contact
        </Button>
      </div>

      {showCreate && (
        <ContactForm onClose={() => setShowCreate(false)} />
      )}

      {editingContact && (
        <ContactDetail
          contact={editingContact}
          onClose={() => setEditingContact(null)}
        />
      )}

      {contacts?.length === 0 ? (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription>No contacts configured. Add a contact to set up notification routing.</AlertDescription>
        </Alert>
      ) : (
        <div className="space-y-3">
          {contacts?.map(contact => (
            <Card key={contact.id} className="py-0">
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <h2 className="text-lg font-semibold text-foreground">{contact.name}</h2>
                      {contact.isDefault && (
                        <Badge variant="secondary" className="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200">Default</Badge>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      {contact.channels.length} channel{contact.channels.length !== 1 ? 's' : ''} |{' '}
                      {contact.serviceCount} service{contact.serviceCount !== 1 ? 's' : ''}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => setEditingContact(contact)}
                    >
                      Manage
                    </Button>
                    {!contact.isDefault && (
                      <AlertDialog>
                        <AlertDialogTrigger asChild>
                          <Button variant="destructive" size="sm">Delete</Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                          <AlertDialogHeader>
                            <AlertDialogTitle>Delete contact "{contact.name}"?</AlertDialogTitle>
                            <AlertDialogDescription>
                              This will remove the contact and all its channels.
                            </AlertDialogDescription>
                          </AlertDialogHeader>
                          <AlertDialogFooter>
                            <AlertDialogCancel>Cancel</AlertDialogCancel>
                            <AlertDialogAction onClick={() => deleteMutation.mutate(contact.id)}>
                              Delete
                            </AlertDialogAction>
                          </AlertDialogFooter>
                        </AlertDialogContent>
                      </AlertDialog>
                    )}
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

function ContactForm({ onClose, contact }: { onClose: () => void; contact?: Contact }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(contact?.name ?? '');

  const createMutation = useMutation({
    mutationFn: (name: string) => contactsApi.create(name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Contact created');
      onClose();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to create contact'));
    },
  });

  const updateMutation = useMutation({
    mutationFn: (name: string) => contactsApi.update(contact!.id, name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Contact updated');
      onClose();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update contact'));
    },
  });

  return (
    <Card className="border-blue-200 dark:border-blue-800 py-0">
      <CardContent className="p-6">
        <h3 className="text-lg font-semibold mb-3">{contact ? 'Edit Contact' : 'New Contact'}</h3>
        <div className="flex gap-3">
          <Input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="Contact name"
            className="flex-1"
          />
          <Button
            onClick={() => contact ? updateMutation.mutate(name) : createMutation.mutate(name)}
            disabled={!name.trim()}
          >
            {contact ? 'Save' : 'Create'}
          </Button>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function ContactDetail({ contact, onClose }: { contact: Contact; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [showAddChannel, setShowAddChannel] = useState(false);
  const [editingName, setEditingName] = useState(false);
  const [name, setName] = useState(contact.name);
  const [channelToRemove, setChannelToRemove] = useState<string | null>(null);

  const { data: freshContact } = useQuery({
    queryKey: ['contacts', contact.id],
    queryFn: () => contactsApi.get(contact.id),
    initialData: contact,
  });

  const updateNameMutation = useMutation({
    mutationFn: (newName: string) => contactsApi.update(contact.id, newName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      setEditingName(false);
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update contact name'));
    },
  });

  const deleteChannelMutation = useMutation({
    mutationFn: (channelId: string) => contactsApi.deleteChannel(contact.id, channelId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Channel removed');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to remove channel'));
    },
  });

  const toggleChannelMutation = useMutation({
    mutationFn: (ch: ContactChannel) =>
      contactsApi.updateChannel(contact.id, ch.id, ch.configuration, !ch.isEnabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contacts'] }),
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to update channel'));
    },
  });

  const testChannelMutation = useMutation({
    mutationFn: (channelId: string) => contactsApi.testChannel(contact.id, channelId),
    onSuccess: () => {
      toast.success('Test notification sent');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to send test notification'));
    },
  });

  const displayContact = freshContact ?? contact;

  return (
    <Card className="border-blue-200 dark:border-blue-800 py-0">
      <CardContent className="p-6">
      <div className="flex items-center justify-between mb-4">
        {editingName ? (
          <div className="flex gap-2">
            <Input
              type="text"
              value={name}
              onChange={e => setName(e.target.value)}
              className="h-8"
            />
            <Button
              size="sm"
              onClick={() => updateNameMutation.mutate(name)}
            >
              Save
            </Button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-semibold">{displayContact.name}</h2>
            <Button
              variant="link"
              size="sm"
              className="p-0 h-auto"
              onClick={() => setEditingName(true)}
            >
              Edit
            </Button>
          </div>
        )}
        <Button variant="ghost" size="sm" onClick={onClose}>
          Close
        </Button>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="font-medium text-foreground">Channels</h3>
          <Button
            variant="link"
            size="sm"
            className="p-0 h-auto"
            onClick={() => setShowAddChannel(true)}
          >
            + Add Channel
          </Button>
        </div>

        {showAddChannel && (
          <AddChannelForm
            contactId={contact.id}
            onClose={() => setShowAddChannel(false)}
          />
        )}

        {displayContact.channels.length === 0 ? (
          <p className="text-sm text-muted-foreground">No channels configured.</p>
        ) : (
          displayContact.channels.map(ch => (
            <div key={ch.id} className="flex items-center justify-between p-3 bg-muted rounded">
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium">
                  {ch.type === ChannelType.Telegram ? 'Telegram' : 'Email'}
                </span>
                <div className="flex items-center gap-2">
                  <Switch
                    checked={ch.isEnabled}
                    onCheckedChange={() => toggleChannelMutation.mutate(ch)}
                  />
                  <span className={`text-xs ${ch.isEnabled ? 'text-green-600 dark:text-green-400' : 'text-muted-foreground'}`}>
                    {ch.isEnabled ? 'Enabled' : 'Disabled'}
                  </span>
                </div>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => testChannelMutation.mutate(ch.id)}>
                    Send Test
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-red-600 focus:text-red-600"
                    onClick={() => setChannelToRemove(ch.id)}
                  >
                    Remove
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          ))
        )}

        <AlertDialog open={!!channelToRemove} onOpenChange={() => setChannelToRemove(null)}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Remove this channel?</AlertDialogTitle>
              <AlertDialogDescription>
                This will stop notifications via this channel.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction onClick={() => { if (channelToRemove) deleteChannelMutation.mutate(channelToRemove); }}>
                Remove
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
      </CardContent>
    </Card>
  );
}

function AddChannelForm({ contactId, onClose }: { contactId: string; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [type, setType] = useState<ChannelType>(ChannelType.Telegram);
  const [botToken, setBotToken] = useState('');
  const [chatId, setChatId] = useState('');

  const addMutation = useMutation({
    mutationFn: () => {
      const config = type === ChannelType.Telegram
        ? JSON.stringify({ botToken, chatId })
        : '{}';
      return contactsApi.addChannel(contactId, type, config);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      toast.success('Channel added');
      onClose();
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'Failed to add channel'));
    },
  });

  return (
    <div className="p-3 border rounded space-y-3">
      <div className="space-y-2">
        <Label>Type</Label>
        <Select value={String(type)} onValueChange={v => setType(Number(v) as ChannelType)}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={String(ChannelType.Telegram)}>Telegram</SelectItem>
            <SelectItem value={String(ChannelType.Email)}>Email</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {type === ChannelType.Telegram && (
        <>
          <div className="space-y-2">
            <Label>Bot Token</Label>
            <Input
              type="text"
              value={botToken}
              onChange={e => setBotToken(e.target.value)}
              placeholder="123456:ABC-DEF..."
              className="text-sm"
            />
          </div>
          <div className="space-y-2">
            <Label>Chat ID</Label>
            <Input
              type="text"
              value={chatId}
              onChange={e => setChatId(e.target.value)}
              placeholder="-100123456789"
              className="text-sm"
            />
          </div>
        </>
      )}

      <div className="flex gap-2">
        <Button
          size="sm"
          onClick={() => addMutation.mutate()}
          disabled={type === ChannelType.Telegram && (!botToken || !chatId)}
        >
          Add Channel
        </Button>
        <Button variant="ghost" size="sm" onClick={onClose}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
