using System;
using System.Runtime.Intrinsics.Arm;
using APIServer.Model;
using APIServer.Services.Contract;

namespace APIServer.Services;

/// <summary>
/// Backgroud service to mimic order placed
/// </summary>
/// <param name="eventStore"></param>
/// <param name="eventBroadcaster"></param>
public class OrderEventEmitter(IEventStore<Order> eventStore, IEventBroadcaster<Order> eventBroadcaster) : BackgroundService
{
    public required IEventStore<Order> _eventStore = eventStore;
    public required IEventBroadcaster<Order> _eventBroadcaster = eventBroadcaster;
    private long _idCounter = 0;
    private List<string> Items {get; set;} = ["Macbook", "Dell", "HP", "Acer"];
    private List<string> Status {get; set;} = ["SUCCESS", "PENDING", "CANCELLED"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            SseEvent<Order>? evt = new()
            {
              EventID = Interlocked.Increment(ref _idCounter),
              EventType = "Order",
              Data = new()
              {
                ItemName = Items[Random.Shared.Next(Items.Count)],
                Quantity = Random.Shared.Next(1, 10),
                Status = Status[Random.Shared.Next(Status.Count)],
                Price = Random.Shared.NextDouble() * Random.Shared.Next(1, 100)
              }  
            };

            _eventStore.AddEvent(evt);
            await _eventBroadcaster.PublishAsync(evt);
            await Task.Delay(5000, stoppingToken);
        }
    }
}