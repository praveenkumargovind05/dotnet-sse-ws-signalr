using System.Threading.Channels;
using APIServer.Model;

namespace APIServer.Services.Contract;

public interface IEventBroadcaster<TData>
{
    Task PublishAsync(SseEvent<TData> evt);
    ChannelReader<SseEvent<TData>> SubscribeReader();
}
