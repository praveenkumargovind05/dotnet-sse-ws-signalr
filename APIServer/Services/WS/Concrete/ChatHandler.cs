using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using APIServer.Model.WS;
using APIServer.Services.WS.Contract;

namespace APIServer.Services.WS.Concrete;

public class ChatHandler(IConnectionManager connectionManager) : IChatHandler
{
    public required IConnectionManager _connectionManager = connectionManager;
    public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        string wsID = _connectionManager.AddSocket(webSocket);
        byte[] bytes = new byte[4096];
        MemoryStream? messageBuffer = new();

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult? webSocketReceiveResult;
            do
            {
                webSocketReceiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(bytes), cancellationToken);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", cancellationToken);
                    _connectionManager.RemoveSocketByID(wsID);
                    return;
                }
                await messageBuffer.WriteAsync(bytes.AsMemory(0, webSocketReceiveResult.Count), cancellationToken);
            } while (!webSocketReceiveResult.EndOfMessage);

            var message = Encoding.UTF8.GetString(messageBuffer.ToArray(), 0, (int)messageBuffer.Length);

            messageBuffer.SetLength(0);
            await BroadcastAsync(message, wsID, cancellationToken);
        }
    }

    public async Task SendMessageAsync(string wsID, TaskDetail task, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Serialize(task);
        var bytes = Encoding.UTF8.GetBytes(message);
        var ws = _connectionManager.GetWebSocketByID(wsID);
        if (ws is null)
            return;
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }
    
    public async Task BroadcastAsync(string message, string excludeWsID, CancellationToken cancellationToken)
    {
        SocketMessage? json = null;
        try
        {
            json = JsonSerializer.Deserialize<SocketMessage>(message);
        }
        catch(JsonException)
        {
            // Ignore
        }
        switch (json?.Type?.ToUpper())
        {
            case "ORDER":
                var payload = json.Payload.Deserialize<TaskDetail>() ?? new();
                payload.CompleteBy = DateTime.UtcNow;
                message = JsonSerializer.Serialize(payload);
                break;
        }
        var bytes = Encoding.UTF8.GetBytes(message);
        var tasks = _connectionManager
        .GetAllSockets(excludeWsID)
        .Where(ws => ws.State == WebSocketState.Open)
        .Select(ws =>
            ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken));

        await Task.WhenAll(tasks);
    }
}
