# Server-Sent Events (SSE) in .NET 8  
## Production-Grade Implementation with Reconnect & Replay

---

# 1. What is SSE?

Server-Sent Events (SSE) is a **unidirectional streaming protocol over HTTP**.

- Server → Client only
- Uses standard HTTP
- Content-Type: `text/event-stream`
- Automatically reconnects
- Supports event IDs for replay

Unlike WebSockets:
- No protocol upgrade
- No bidirectional channel
- Simpler infra
- Works behind most proxies/load balancers

---

# 2. SSE Protocol Format (Wire Level)

Every message must follow this format:

```
id: 123
event: tick
data: some payload
data: multi-line supported

```

Rules:
- Each field is newline terminated
- Blank line ends one event
- `data:` can appear multiple times
- `id:` enables reconnect support
- `:` prefix = comment (used for heartbeat)

Example heartbeat:
```
: heartbeat

```

---

# 3. Reconnect Behavior

Browser `EventSource` automatically:

- Reconnects if connection drops
- Sends header:
  
```
Last-Event-ID: <last_received_id>
```

Server must:
1. Parse that header
2. Replay missed events
3. Continue live stream

If you don’t handle this → you lose events.

---

# 4. Architecture (Production-Ready Design)

```
Client (EventSource)
      ↓
SSE Endpoint (/events)
      ↓
Replay Store (bounded history)
      ↓
Live Broadcaster (Channel<T>)
      ↓
Event Producer (BackgroundService)
```

Design principles:

- Monotonic event IDs
- Bounded memory
- Backpressure control
- Proper cancellation
- Heartbeat support

---

# 5. Complete .NET 8 Implementation

---

## 5.1 Event Model

```csharp
public record SseEvent(
    long Id,
    string EventType,
    string Data,
    DateTime CreatedAtUtc
);
```

---

## 5.2 Replay Store (In-Memory, Bounded)

⚠ Replace with Redis/Kafka in distributed production.

```csharp
using System.Collections.Concurrent;

public interface IEventStore
{
    void Add(SseEvent evt);
    IReadOnlyList<SseEvent> GetSince(long lastEventId);
}

public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentQueue<SseEvent> _events = new();
    private const int MaxEvents = 10000;

    public void Add(SseEvent evt)
    {
        _events.Enqueue(evt);

        while (_events.Count > MaxEvents && _events.TryDequeue(out _))
        {
            // Trim oldest
        }
    }

    public IReadOnlyList<SseEvent> GetSince(long lastEventId)
    {
        return _events
            .Where(e => e.Id > lastEventId)
            .OrderBy(e => e.Id)
            .ToList();
    }
}
```

---

## 5.3 Live Broadcaster (Backpressure Safe)

```csharp
using System.Threading.Channels;

public interface IEventBroadcaster
{
    ValueTask PublishAsync(SseEvent evt);
    ChannelReader<SseEvent> Subscribe();
}

public class EventBroadcaster : IEventBroadcaster
{
    private readonly Channel<SseEvent> _channel =
        Channel.CreateBounded<SseEvent>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public async ValueTask PublishAsync(SseEvent evt)
    {
        await _channel.Writer.WriteAsync(evt);
    }

    public ChannelReader<SseEvent> Subscribe() => _channel.Reader;
}
```

---

## 5.4 Background Event Generator

```csharp
public class DemoEventGenerator : BackgroundService
{
    private readonly IEventBroadcaster _broadcaster;
    private readonly IEventStore _store;
    private long _idCounter = 0;

    public DemoEventGenerator(
        IEventBroadcaster broadcaster,
        IEventStore store)
    {
        _broadcaster = broadcaster;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var evt = new SseEvent(
                Id: Interlocked.Increment(ref _idCounter),
                EventType: "tick",
                Data: $"Server time {DateTime.UtcNow:O}",
                CreatedAtUtc: DateTime.UtcNow
            );

            _store.Add(evt);
            await _broadcaster.PublishAsync(evt);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## 5.5 SSE Endpoint

```csharp
app.MapGet("/events", async (
    HttpContext context,
    IEventStore store,
    IEventBroadcaster broadcaster) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    var cancellation = context.RequestAborted;

    var lastEventIdHeader = context.Request.Headers["Last-Event-ID"].FirstOrDefault();
    long lastEventId = 0;

    if (!string.IsNullOrWhiteSpace(lastEventIdHeader))
        long.TryParse(lastEventIdHeader, out lastEventId);

    // Replay missed events
    var missedEvents = store.GetSince(lastEventId);

    foreach (var evt in missedEvents)
        await WriteEventAsync(context.Response, evt, cancellation);

    // Subscribe to live stream
    var reader = broadcaster.Subscribe();

    // Heartbeat loop
    _ = Task.Run(async () =>
    {
        while (!cancellation.IsCancellationRequested)
        {
            await context.Response.WriteAsync(": heartbeat\n\n", cancellation);
            await context.Response.Body.FlushAsync(cancellation);
            await Task.Delay(TimeSpan.FromSeconds(15), cancellation);
        }
    }, cancellation);

    await foreach (var evt in reader.ReadAllAsync(cancellation))
    {
        await WriteEventAsync(context.Response, evt, cancellation);
    }
});
```

---

## 5.6 SSE Writer Helper

```csharp
static async Task WriteEventAsync(
    HttpResponse response,
    SseEvent evt,
    CancellationToken cancellation)
{
    await response.WriteAsync($"id: {evt.Id}\n", cancellation);
    await response.WriteAsync($"event: {evt.EventType}\n", cancellation);

    var lines = evt.Data.Split('\n');

    foreach (var line in lines)
        await response.WriteAsync($"data: {line}\n", cancellation);

    await response.WriteAsync("\n", cancellation);
    await response.Body.FlushAsync(cancellation);
}
```

---

## 5.7 DI Registration

```csharp
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddSingleton<IEventBroadcaster, EventBroadcaster>();
builder.Services.AddHostedService<DemoEventGenerator>();
```

---

# 6. Browser Client Example

```javascript
const source = new EventSource("/events");

source.onmessage = (event) => {
    console.log(event.lastEventId, event.data);
};

source.onerror = (err) => {
    console.error("Connection lost. Reconnecting...");
};
```

Reconnect is automatic.

---

# 7. Production Hardening Checklist

### Reverse Proxy

Disable buffering (Nginx example):

```
proxy_buffering off;
```

### Load Balancer

- Increase idle timeout
- Enable HTTP/1.1 keep-alive

### Memory Control

- Bound replay size
- Avoid infinite history

### Scaling

Single instance = in-memory OK

Multiple instances = use:

- Redis sorted set
- Kafka topic
- PostgreSQL append-only log

---

# 8. When to Use SSE

Use SSE for:

- Live dashboards
- Real-time logs
- Notifications
- Market feeds
- Build status streams

Do NOT use SSE for:

- Chat apps
- Multiplayer games
- Bidirectional messaging

Use WebSockets or SignalR instead.

---

# 9. Performance Considerations

- Each client = 1 open HTTP connection
- 10k clients = 10k open sockets
- Use Kestrel tuning
- Monitor:
  - Connection count
  - Memory
  - Thread pool starvation

---

# 10. Key Design Takeaways

- Always implement `Last-Event-ID`
- Always bound memory
- Always flush response
- Always handle cancellation
- Always add heartbeat
- Never trust infinite in-memory replay

---

# 11. Summary

.NET 8 does NOT provide native SSE abstraction.

You must manually handle:

- Protocol framing
- Reconnect logic
- Replay buffer
- Backpressure
- Heartbeat
- Cancellation

This implementation gives:

- Ordered delivery
- Deterministic replay
- Backpressure safety
- Production readiness

---

END OF DOCUMENT