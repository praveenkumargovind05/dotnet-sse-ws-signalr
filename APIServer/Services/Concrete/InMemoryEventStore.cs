using System;
using System.Collections.Concurrent;
using APIServer.Model;
using APIServer.Services.Contract;

namespace APIServer.Services.Concrete;

/// <summary>
/// To store the SSE Item for replay of client missed 
/// </summary>
/// <typeparam name="TData"></typeparam>
public class InMemoryEventStore<TData> : IEventStore<TData>
{
    private readonly ConcurrentQueue<SseEvent<TData>> _events = new();
    private const int MaxEventLimit = 100;
    public void AddEvent(SseEvent<TData> evt)
    {
        _events.Enqueue(evt);

        while (_events.Count > MaxEventLimit && _events.TryDequeue(out _))
        {
        }
        // while (_events.Count > MaxEventLimit)
        //     _events.TryDequeue(out _);
    }

    public IReadOnlyList<SseEvent<TData>> GetEventFrom(long eventID)
    {
        return
        [..
        _events.Where(evt => evt.EventID>eventID)
        .OrderBy(evt => evt.EventID)];
    }
}
