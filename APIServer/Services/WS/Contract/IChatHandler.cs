using System;
using System.Net.WebSockets;
using APIServer.Model.WS;

namespace APIServer.Services.WS.Contract;

public interface IChatHandler
{
    Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken);
    Task SendMessageAsync(string wsID, TaskDetail task, CancellationToken cancellationToken);
    Task BroadcastAsync(string message, string excludeWsID, CancellationToken cancellationToken);
}
