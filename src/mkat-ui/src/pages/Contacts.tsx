import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contactsApi } from '../api/services';
import { ChannelType } from '../api/types';
import type { Contact, ContactChannel } from '../api/types';

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
        <button
          onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Add Contact
        </button>
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
        <div className="bg-white rounded-lg shadow p-6 text-center text-gray-500">
          No contacts configured. Add a contact to set up notification routing.
        </div>
      ) : (
        <div className="space-y-3">
          {contacts?.map(contact => (
            <div key={contact.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-lg font-semibold text-gray-900">{contact.name}</h2>
                    {contact.isDefault && (
                      <span className="px-2 py-0.5 text-xs bg-blue-100 text-blue-800 rounded">Default</span>
                    )}
                  </div>
                  <p className="text-sm text-gray-500 mt-1">
                    {contact.channels.length} channel{contact.channels.length !== 1 ? 's' : ''} |{' '}
                    {contact.serviceCount} service{contact.serviceCount !== 1 ? 's' : ''}
                  </p>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => setEditingContact(contact)}
                    className="px-3 py-1.5 text-sm bg-gray-100 text-gray-800 rounded hover:bg-gray-200"
                  >
                    Manage
                  </button>
                  {!contact.isDefault && (
                    <button
                      onClick={() => {
                        if (confirm(`Delete contact "${contact.name}"?`)) {
                          deleteMutation.mutate(contact.id);
                        }
                      }}
                      className="px-3 py-1.5 text-sm bg-red-100 text-red-800 rounded hover:bg-red-200"
                    >
                      Delete
                    </button>
                  )}
                </div>
              </div>
            </div>
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
    <div className="bg-white rounded-lg shadow p-6 border border-blue-200">
      <h3 className="text-lg font-semibold mb-3">{contact ? 'Edit Contact' : 'New Contact'}</h3>
      <div className="flex gap-3">
        <input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder="Contact name"
          className="flex-1 rounded border-gray-300 shadow-sm px-3 py-2 border"
        />
        <button
          onClick={() => contact ? updateMutation.mutate(name) : createMutation.mutate(name)}
          disabled={!name.trim()}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {contact ? 'Save' : 'Create'}
        </button>
        <button onClick={onClose} className="px-4 py-2 text-gray-600 hover:text-gray-800">
          Cancel
        </button>
      </div>
    </div>
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
    <div className="bg-white rounded-lg shadow p-6 border border-blue-200">
      <div className="flex items-center justify-between mb-4">
        {editingName ? (
          <div className="flex gap-2">
            <input
              type="text"
              value={name}
              onChange={e => setName(e.target.value)}
              className="rounded border-gray-300 shadow-sm px-3 py-1 border"
            />
            <button
              onClick={() => updateNameMutation.mutate(name)}
              className="px-3 py-1 text-sm bg-blue-600 text-white rounded"
            >
              Save
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-semibold">{displayContact.name}</h2>
            <button
              onClick={() => setEditingName(true)}
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              Edit
            </button>
          </div>
        )}
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
          Close
        </button>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="font-medium text-gray-700">Channels</h3>
          <button
            onClick={() => setShowAddChannel(true)}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            + Add Channel
          </button>
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
                <span className={`ml-2 text-xs ${ch.isEnabled ? 'text-green-600' : 'text-gray-400'}`}>
                  {ch.isEnabled ? 'Enabled' : 'Disabled'}
                </span>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => testChannelMutation.mutate(ch.id)}
                  className="text-xs px-2 py-1 bg-blue-50 text-blue-700 rounded hover:bg-blue-100"
                >
                  Test
                </button>
                <button
                  onClick={() => toggleChannelMutation.mutate(ch)}
                  className="text-xs px-2 py-1 bg-gray-100 rounded hover:bg-gray-200"
                >
                  {ch.isEnabled ? 'Disable' : 'Enable'}
                </button>
                <button
                  onClick={() => {
                    if (confirm('Remove this channel?')) {
                      deleteChannelMutation.mutate(ch.id);
                    }
                  }}
                  className="text-xs px-2 py-1 bg-red-50 text-red-700 rounded hover:bg-red-100"
                >
                  Remove
                </button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
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
      <div>
        <label className="block text-sm font-medium text-gray-700">Type</label>
        <select
          value={type}
          onChange={e => setType(Number(e.target.value))}
          className="mt-1 block w-full rounded border-gray-300 shadow-sm px-3 py-2 border"
        >
          <option value={ChannelType.Telegram}>Telegram</option>
          <option value={ChannelType.Email}>Email</option>
        </select>
      </div>

      {type === ChannelType.Telegram && (
        <>
          <div>
            <label className="block text-sm font-medium text-gray-700">Bot Token</label>
            <input
              type="text"
              value={botToken}
              onChange={e => setBotToken(e.target.value)}
              placeholder="123456:ABC-DEF..."
              className="mt-1 block w-full rounded border-gray-300 shadow-sm px-3 py-2 border text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Chat ID</label>
            <input
              type="text"
              value={chatId}
              onChange={e => setChatId(e.target.value)}
              placeholder="-100123456789"
              className="mt-1 block w-full rounded border-gray-300 shadow-sm px-3 py-2 border text-sm"
            />
          </div>
        </>
      )}

      <div className="flex gap-2">
        <button
          onClick={() => addMutation.mutate()}
          disabled={type === ChannelType.Telegram && (!botToken || !chatId)}
          className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          Add Channel
        </button>
        <button onClick={onClose} className="px-3 py-1.5 text-sm text-gray-600 hover:text-gray-800">
          Cancel
        </button>
      </div>
    </div>
  );
}
