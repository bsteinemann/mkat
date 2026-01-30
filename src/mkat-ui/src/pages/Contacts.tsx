import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contactsApi } from '../api/services';
import { ChannelType } from '../api/types';
import type { Contact, ContactChannel } from '../api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contacts'] }),
  });

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Contacts</h1>
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
        <Card className="py-0">
          <CardContent className="p-6 text-center text-gray-500">
            No contacts configured. Add a contact to set up notification routing.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {contacts?.map(contact => (
            <Card key={contact.id} className="py-0">
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <h2 className="text-lg font-semibold text-gray-900">{contact.name}</h2>
                      {contact.isDefault && (
                        <Badge variant="secondary" className="bg-blue-100 text-blue-800">Default</Badge>
                      )}
                    </div>
                    <p className="text-sm text-gray-500 mt-1">
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
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => {
                          if (confirm(`Delete contact "${contact.name}"?`)) {
                            deleteMutation.mutate(contact.id);
                          }
                        }}
                      >
                        Delete
                      </Button>
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
      onClose();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (name: string) => contactsApi.update(contact!.id, name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      onClose();
    },
  });

  return (
    <Card className="border-blue-200 py-0">
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
  });

  const deleteChannelMutation = useMutation({
    mutationFn: (channelId: string) => contactsApi.deleteChannel(contact.id, channelId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contacts'] }),
  });

  const toggleChannelMutation = useMutation({
    mutationFn: (ch: ContactChannel) =>
      contactsApi.updateChannel(contact.id, ch.id, ch.configuration, !ch.isEnabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contacts'] }),
  });

  const testChannelMutation = useMutation({
    mutationFn: (channelId: string) => contactsApi.testChannel(contact.id, channelId),
  });

  const displayContact = freshContact ?? contact;

  return (
    <Card className="border-blue-200 py-0">
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
          <h3 className="font-medium text-gray-700">Channels</h3>
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
          <p className="text-sm text-gray-500">No channels configured.</p>
        ) : (
          displayContact.channels.map(ch => (
            <div key={ch.id} className="flex items-center justify-between p-3 bg-gray-50 rounded">
              <div>
                <span className="text-sm font-medium">
                  {ch.type === ChannelType.Telegram ? 'Telegram' : 'Email'}
                </span>
                <Badge variant="outline" className={`ml-2 ${ch.isEnabled ? 'text-green-600' : 'text-gray-400'}`}>
                  {ch.isEnabled ? 'Enabled' : 'Disabled'}
                </Badge>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="xs"
                  onClick={() => testChannelMutation.mutate(ch.id)}
                >
                  Test
                </Button>
                <Button
                  variant="outline"
                  size="xs"
                  onClick={() => toggleChannelMutation.mutate(ch)}
                >
                  {ch.isEnabled ? 'Disable' : 'Enable'}
                </Button>
                <Button
                  variant="destructive"
                  size="xs"
                  onClick={() => {
                    if (confirm('Remove this channel?')) {
                      deleteChannelMutation.mutate(ch.id);
                    }
                  }}
                >
                  Remove
                </Button>
              </div>
            </div>
          ))
        )}
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
      onClose();
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
