using System;
using System.Runtime.Intrinsics.X86;
using APIServer.Model.SSE;

namespace APIServer.Services.SSE.Contract;

public interface IEventStore<TData>
{
    void AddEvent(SseEvent<TData> evt);
    IReadOnlyList<SseEvent<TData>> GetEventFrom(long eventID);
}
