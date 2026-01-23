using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Contact?> GetByIdWithChannelsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken ct = default);
    Task<Contact?> GetDefaultAsync(CancellationToken ct = default);
    Task AddAsync(Contact contact, CancellationToken ct = default);
    Task UpdateAsync(Contact contact, CancellationToken ct = default);
    Task DeleteAsync(Contact contact, CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task SetServiceContactsAsync(Guid serviceId, IEnumerable<Guid> contactIds, CancellationToken ct = default);
    Task<bool> IsOnlyContactForAnyServiceAsync(Guid contactId, CancellationToken ct = default);
}
