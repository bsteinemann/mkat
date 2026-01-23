using Microsoft.EntityFrameworkCore;
using Mkat.Application.Interfaces;
using Mkat.Domain.Entities;
using Mkat.Infrastructure.Data;

namespace Mkat.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly MkatDbContext _context;

    public ContactRepository(MkatDbContext context)
    {
        _context = context;
    }

    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Contact?> GetByIdWithChannelsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Contacts
            .Include(c => c.Channels)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Contacts
            .Include(c => c.Channels)
            .Include(c => c.ServiceContacts)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Contact?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await _context.Contacts
            .Include(c => c.Channels)
            .FirstOrDefaultAsync(c => c.IsDefault, ct);
    }

    public async Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        await _context.Contacts.AddAsync(contact, ct);
    }

    public async Task UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        _context.Contacts.Update(contact);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Contact contact, CancellationToken ct = default)
    {
        _context.Contacts.Remove(contact);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Contact>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await _context.ServiceContacts
            .Where(sc => sc.ServiceId == serviceId)
            .Include(sc => sc.Contact!)
                .ThenInclude(c => c.Channels)
            .Select(sc => sc.Contact!)
            .ToListAsync(ct);
    }

    public async Task SetServiceContactsAsync(Guid serviceId, IEnumerable<Guid> contactIds, CancellationToken ct = default)
    {
        var existing = await _context.ServiceContacts
            .Where(sc => sc.ServiceId == serviceId)
            .ToListAsync(ct);

        _context.ServiceContacts.RemoveRange(existing);

        var newLinks = contactIds.Select(cid => new ServiceContact
        {
            ServiceId = serviceId,
            ContactId = cid
        });

        await _context.ServiceContacts.AddRangeAsync(newLinks, ct);
    }

    public async Task AddChannelAsync(ContactChannel channel, CancellationToken ct = default)
    {
        await _context.ContactChannels.AddAsync(channel, ct);
    }

    public async Task RemoveChannelAsync(ContactChannel channel, CancellationToken ct = default)
    {
        _context.ContactChannels.Remove(channel);
        await Task.CompletedTask;
    }

    public async Task<bool> IsOnlyContactForAnyServiceAsync(Guid contactId, CancellationToken ct = default)
    {
        var serviceIds = await _context.ServiceContacts
            .Where(sc => sc.ContactId == contactId)
            .Select(sc => sc.ServiceId)
            .ToListAsync(ct);

        foreach (var serviceId in serviceIds)
        {
            var count = await _context.ServiceContacts
                .CountAsync(sc => sc.ServiceId == serviceId, ct);
            if (count <= 1)
                return true;
        }

        return false;
    }
}
