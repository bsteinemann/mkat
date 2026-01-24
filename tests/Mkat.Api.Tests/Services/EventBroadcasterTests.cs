using Mkat.Application.DTOs;
using Mkat.Infrastructure.Services;
using Xunit;

namespace Mkat.Api.Tests.Services;

public class EventBroadcasterTests
{
    [Fact]
    public async Task Broadcast_DeliversToSubscriber()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var received = new List<ServerEvent>();

        var readTask = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received.Add(evt);
                if (received.Count >= 1) break;
            }
        });

        await Task.Delay(50);

        await broadcaster.BroadcastAsync(new ServerEvent { Type = "test", Payload = "hello" });

        await Task.WhenAny(readTask, Task.Delay(1000));
        cts.Cancel();

        Assert.Single(received);
        Assert.Equal("test", received[0].Type);
        Assert.Equal("hello", received[0].Payload);
    }

    [Fact]
    public async Task Broadcast_DeliversToMultipleSubscribers()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var received1 = new List<ServerEvent>();
        var received2 = new List<ServerEvent>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received1.Add(evt);
                if (received1.Count >= 1) break;
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var evt in broadcaster.Subscribe(cts.Token))
            {
                received2.Add(evt);
                if (received2.Count >= 1) break;
            }
        });

        await Task.Delay(50);

        await broadcaster.BroadcastAsync(new ServerEvent { Type = "alert", Payload = "{}" });

        await Task.WhenAny(Task.WhenAll(task1, task2), Task.Delay(1000));
        cts.Cancel();

        Assert.Single(received1);
        Assert.Single(received2);
    }

    [Fact]
    public async Task Subscribe_CompletesOnCancellation()
    {
        var broadcaster = new EventBroadcaster();
        var cts = new CancellationTokenSource();
        var count = 0;

        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in broadcaster.Subscribe(cts.Token))
            {
                count++;
            }
        });

        await Task.Delay(50);
        cts.Cancel();

        await Task.WhenAny(readTask, Task.Delay(1000));
        Assert.Equal(0, count);
    }
}
