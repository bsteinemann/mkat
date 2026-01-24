using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Mkat.Application.DTOs;
using Mkat.Application.Interfaces;

namespace Mkat.Infrastructure.Services;

public class EventBroadcaster : IEventBroadcaster
{
    private readonly List<Channel<ServerEvent>> _subscribers = new();
    private readonly object _lock = new();

    public Task BroadcastAsync(ServerEvent serverEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            foreach (var channel in _subscribers)
            {
                channel.Writer.TryWrite(serverEvent);
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ServerEvent> Subscribe(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateBounded<ServerEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                yield return evt;
            }
        }
        finally
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
        }
    }
}
