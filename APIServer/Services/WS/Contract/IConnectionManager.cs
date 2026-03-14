using System;
using System.Net.WebSockets;

namespace APIServer.Services.WS.Contract;

public interface IConnectionManager
{
    string AddSocket(WebSocket socket);
    List<string> GetAllSocketID();
    IEnumerable<WebSocket> GetAllSockets(string excludeWsID = "");
    WebSocket? GetWebSocketByID(string wsID);
    void RemoveSocketByID(string id);
}
