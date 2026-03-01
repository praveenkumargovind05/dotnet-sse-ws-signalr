using System;
using System.Runtime.Intrinsics.X86;
using System.Threading.Channels;
using APIServer.Model;
using APIServer.Services.Contract;

namespace APIServer.Services.Concrete;

/// <summary>
/// Helper class to add and read data from channel
/// </summary>
/// <typeparam name="TData"></typeparam>
public class EventBroacaster<TData> : IEventBroadcaster<TData>
{
    private readonly Channel<SseEvent<TData>> _channel = 
    Channel.CreateBounded<SseEvent<TData>>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    public async Task PublishAsync(SseEvent<TData> evt)
    {
        await _channel.Writer.WriteAsync(evt);
    }

    public ChannelReader<SseEvent<TData>> SubscribeReader()
    {
        return _channel.Reader;
    }
}
