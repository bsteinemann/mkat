using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface IContactChannelSender
{
    Task<bool> SendAlertAsync(ContactChannel channel, Alert alert, Service service, CancellationToken ct = default);
}
