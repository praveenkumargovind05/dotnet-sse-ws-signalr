using System;

namespace APIServer.Model.SSE;

public record SseEvent<T>
{
    public long EventID { get; set; }
    public string? EventType { get; set; }
    public T? Data { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
