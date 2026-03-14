using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using APIServer.Services.WS.Contract;

namespace APIServer.Services.WS.Concrete;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _clientSockets = [];
    public string AddSocket(WebSocket socket)
    {
        var socketID = Guid.NewGuid().ToString();
        _clientSockets.TryAdd(socketID, socket);
        return socketID;
    }

    public IEnumerable<WebSocket> GetAllSockets(string excludeWsID = "")
    {
        return _clientSockets
            .Where(x => x.Key != excludeWsID)
            .Select(x => x.Value);
    }

    public List<string> GetAllSocketID() => [.. _clientSockets.Keys];

    public WebSocket? GetWebSocketByID(string wsID) => _clientSockets.TryGetValue(wsID, out var ws) ? ws : null;

    public void RemoveSocketByID(string id)
    {
        _clientSockets.TryRemove(id, out _);
    }
}
