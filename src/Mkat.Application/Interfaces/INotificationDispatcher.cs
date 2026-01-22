using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(Alert alert, CancellationToken ct = default);
}
