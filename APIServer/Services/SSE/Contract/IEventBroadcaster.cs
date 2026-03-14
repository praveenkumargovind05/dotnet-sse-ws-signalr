using System.Threading.Channels;
using APIServer.Model.SSE;

namespace APIServer.Services.SSE.Contract;

public interface IEventBroadcaster<TData>
{
    Task PublishAsync(SseEvent<TData> evt);
    ChannelReader<SseEvent<TData>> SubscribeReader();
}
