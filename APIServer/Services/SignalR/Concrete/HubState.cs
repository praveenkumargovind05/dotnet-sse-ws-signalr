using System;
using System.Collections.Concurrent;
using APIServer.Controllers;
using APIServer.Model.SignalR;
using APIServer.Services.SignalR.Contract;

namespace APIServer.Services.SignalR.Concrete;

public class HubState : IHubState
{
    private readonly ConcurrentDictionary<string, UserInfo> Users = new();
    
    public void AddOrUpdate(string connectionID, UserInfo userInfo) => Users[connectionID] = userInfo;

    public void Remove(string connectionID) => Users.TryRemove(connectionID, out _);

    public UserInfo? GetUserInfo(string connectionID) => Users.TryGetValue(connectionID, out var user) ? user : null;
    
    public List<UserInfo>? GetAllUsers() => [.. Users.Values];

    public List<UserInfo>? GetUsersInRoom(string roomName) => [.. Users.Values.Where(user => user.RoomName?.Equals(roomName, StringComparison.OrdinalIgnoreCase)??false)];

    public int UserCount() => Users.Count;
}
