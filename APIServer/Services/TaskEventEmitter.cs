using System;
using System.Text.Json;
using APIServer.Model.WS;
using APIServer.Services.WS.Contract;

namespace APIServer.Services;

public class TaskEventEmitter(IConnectionManager connectionManager, IChatHandler chatHandler) : BackgroundService
{
    public readonly IConnectionManager _connectionManager = connectionManager;
    public readonly IChatHandler _chatHandler = chatHandler;
    private List<string> Tasks { get; set; } = ["Add Log", "Security Check", "Free Space"];
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var socketIDs = _connectionManager.GetAllSocketID();
            if (socketIDs.Count != 0)
            {
                var wsID = socketIDs[Random.Shared.Next(socketIDs.Count)];

                var task = new TaskDetail()
                {
                    Title = Tasks[Random.Shared.Next(Tasks.Count)],
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };
                await _chatHandler.SendMessageAsync(wsID, task, stoppingToken);
            }
            await Task.Delay(5000, stoppingToken);
        }

    }
}
