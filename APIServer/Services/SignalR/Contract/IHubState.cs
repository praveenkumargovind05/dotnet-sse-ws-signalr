using System;
using System.Collections.Concurrent;
using APIServer.Controllers;
using APIServer.Model.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace APIServer.Services.SignalR.Contract;

public interface IHubState
{
    void AddOrUpdate(string connectionID, UserInfo userInfo);
    void Remove(string connectionID);
    UserInfo? GetUserInfo(string connectionID);
    List<UserInfo>? GetAllUsers();
    List<UserInfo>? GetUsersInRoom(string roomName);
    int UserCount();
}
