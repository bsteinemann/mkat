using Mkat.Application.DTOs;

namespace Mkat.Application.Interfaces;

public interface IEventBroadcaster
{
    Task BroadcastAsync(ServerEvent serverEvent, CancellationToken ct = default);
    IAsyncEnumerable<ServerEvent> Subscribe(CancellationToken ct = default);
}
