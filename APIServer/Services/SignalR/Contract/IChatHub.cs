using System;
using APIServer.Model.SignalR;

namespace APIServer.Services.SignalR.Contract;

/// <summary>
/// Implementaion for client side
/// </summary>
public interface IChatHub
{
    Task ReceiveMessage(ChatInfo message);
    Task ReceivePrivateMessage(ChatInfo message);
    Task UserJoined(UserInfo userInfo);
    Task UserLeft(UserInfo userInfo);
    Task RoomSnapshot(RoomSnapInfo roomSnapInfo);
    Task OnlineUsers(List<UserInfo> userInfos);
    Task OnlineUsersInGroup(List<UserInfo> userInfos);
    Task ReceiveNotification(string notification);
}
