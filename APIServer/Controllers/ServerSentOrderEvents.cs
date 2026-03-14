using System.Text.Json;
using APIServer.Model.SSE;
using APIServer.Services.SSE.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerSentOrderEvents(IEventStore<Order> eventStore, IEventBroadcaster<Order> eventBroadcaster) : ControllerBase
    {
        public required IEventStore<Order> _eventStore = eventStore;
        public required IEventBroadcaster<Order> _eventBroadcaster = eventBroadcaster;

        [HttpGet("/get-orders")]
        public async Task GetEvents(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                HttpContext.Response.Headers.ContentType = "text/event-stream";
                HttpContext.Response.Headers.CacheControl = "no-cache";
                HttpContext.Response.Headers.Connection = "keep-alive";

                var channelReader = _eventBroadcaster.SubscribeReader();

                // 1. Get events with replay 
                var lastEventIDHeader = HttpContext.Request.Headers["Last-Event-ID"];
                long lastEventID = 0;
                if (long.TryParse(lastEventIDHeader, out var value))
                    lastEventID = value;

                var missedEvents = _eventStore.GetEventFrom(lastEventID);
                foreach (var evt in missedEvents)
                    await WriteEventAsync(HttpContext, evt, cancellationToken);

                // 2. Read Live events with heart beat
                /**
                    Case 1 — Data Arrives Before 15 Seconds
                        . As long as data keeps flowing within every 15 seconds, heartbeat will NEVER fire.
                    Case 2 — No Data for 15 Seconds
                        . Heartbeat only fires during inactivity periods.
                        . Ensure the connection is not idle for more than 15 seconds.
                **/
                var heartBeatInterval = TimeSpan.FromSeconds(15);
                var heartBeatTask = Task.Delay(heartBeatInterval, cancellationToken);
                while(!cancellationToken.IsCancellationRequested)
                {
                    var readerTask = channelReader.WaitToReadAsync(cancellationToken).AsTask();

                    var completed = await Task.WhenAny(readerTask, heartBeatTask);
                    if(completed == readerTask && await readerTask)
                    {
                        while (channelReader.TryRead(out var evt))
                            await WriteEventAsync(HttpContext, evt, cancellationToken);

                        // Reset heartbeat after sending real data
                        heartBeatTask = Task.Delay(heartBeatInterval, cancellationToken);
                    }
                    else
                    {
                        await HttpContext.Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                        await HttpContext.Response.Body.FlushAsync(cancellationToken);
                        heartBeatTask = Task.Delay(heartBeatInterval, cancellationToken);
                    }
                }

                /* Issue in below heart beat ->     
                    - HttpContext Is Request-Scoped HttpContext is only guaranteed to be valid inside the request execution pipeline.
                    - When you do:
                        _ = Task.Run(...)
                        You detach execution from the request context.
                        Even though the request hasn't returned yet, this is undefined behavior in ASP.NET Core. Under load, this will break.
                    - Concurrent Writes to Response Stream
                    Now we have:
                        One loop inside ReadAllAsync
                        One loop inside Task.Run
                        Both writing to the same response stream.
                        That is a race condition.
                */
                // _ = Task.Run(async () =>
                // {
                //     while (!cancellationToken.IsCancellationRequested)
                //     {
                //         await HttpContext.Response.WriteAsync($"id: 1\n", cancellationToken);
                //         await HttpContext.Response.WriteAsync($"event: test\n", cancellationToken);
                //         await HttpContext.Response.Body.FlushAsync(cancellationToken);
                //         await Task.Delay(TimeSpan.FromSeconds(15));
                //     }
                // }, cancellationToken);
                // // 2. Read Live events 
                // await foreach (var evt in channelReader.ReadAllAsync(cancellationToken))
                //     await WriteEventAsync(HttpContext, evt, cancellationToken);

            }
            catch (TaskCanceledException)
            {
                // Ignore 
            }

        }

        [NonAction]
        private async Task WriteEventAsync(HttpContext httpContext, SseEvent<Order> evt, CancellationToken cancellationToken)
        {
            await HttpContext.Response.WriteAsync($"id: {evt.EventID}\n", cancellationToken);
            await HttpContext.Response.WriteAsync($"event: {evt.EventType}\n", cancellationToken);
            await HttpContext.Response.WriteAsync($"data: {JsonSerializer.Serialize(evt.Data)}\n\n", cancellationToken);
            await HttpContext.Response.Body.FlushAsync(cancellationToken);
        }
    }
}
