using Microsoft.AspNetCore.Mvc;
using Mkat.Application.Interfaces;

namespace Mkat.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventBroadcaster _broadcaster;

    public EventsController(IEventBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        await Response.Body.FlushAsync(ct);

        await foreach (var evt in _broadcaster.Subscribe(ct))
        {
            var payload = $"event: {evt.Type}\ndata: {evt.Payload}\n\n";
            await Response.WriteAsync(payload, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
