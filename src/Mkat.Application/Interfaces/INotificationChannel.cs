using Mkat.Domain.Entities;

namespace Mkat.Application.Interfaces;

public interface INotificationChannel
{
    string ChannelType { get; }
    bool IsEnabled { get; }
    Task<bool> SendAlertAsync(Alert alert, Service service, CancellationToken ct = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken ct = default);
}
