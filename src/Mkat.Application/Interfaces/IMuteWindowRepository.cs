using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IMuteWindowRepository
{
    Task<bool> IsServiceMutedAsync(Guid serviceId, DateTime at, CancellationToken ct = default);
    Task<MuteWindow> AddAsync(MuteWindow mute, CancellationToken ct = default);
    Task<IReadOnlyList<MuteWindow>> GetActiveForServiceAsync(Guid serviceId, CancellationToken ct = default);
}
