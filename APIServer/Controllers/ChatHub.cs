using System.Collections.Concurrent;
using System.Globalization;
using APIServer.Model.SignalR;
using APIServer.Services.SignalR.Contract;
using Microsoft.AspNetCore.SignalR;

namespace APIServer.Controllers;

/// <summary>
/// Resposible from server to client
/// without type hub client :
///     Clients.OthersInGroup(userInfo.RoomName!).SendAsync("method-name-client-subscribed", arg1, arg2);
/// </summary>
/// <param name="hubState"></param>
public class ChatHub(IHubState hubState) : Hub<IChatHub>
{
    public required IHubState _hubState = hubState;
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var connectionID = Context.ConnectionId;
        var userName = http?.Request.Query["user-name"].ToString().Trim('"') ?? "Guest";
        var room = http?.Request.Query["room"].ToString().Trim('"') ?? "common";

        var userInfo = new UserInfo()
        {
            ConnectionID = connectionID,
            UserName = userName,
            RoomName = room,
            JoinedOn = DateTime.UtcNow
        };
        _hubState.AddOrUpdate(connectionID, userInfo);

        // Add to SignalR group
        await Groups.AddToGroupAsync(connectionID, userInfo.RoomName!);

        // Send room details to caller subscribed method (RoomSnapshot)
        await Clients.Caller.RoomSnapshot(
            new()
            {
                RoomName = userInfo.RoomName,
                Members = _hubState.GetUsersInRoom(userInfo.RoomName!),
                MemebrCount = _hubState.GetUsersInRoom(userInfo.RoomName!)?.Count ?? 0
            }
        );

        // Tell EVERYONE ELSE in the room a new user joined
        await Clients.OthersInGroup(userInfo.RoomName!).UserJoined(userInfo);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionID = Context.ConnectionId;
        var userInfo = _hubState.GetUserInfo(connectionID);
        if (userInfo is not null)
        {
            _hubState.Remove(connectionID);
            // Tell EVERYONE ELSE in the room a user left
            await Clients.OthersInGroup(userInfo.RoomName!).UserLeft(userInfo);
        }
    }

    public async Task SendRoomMessage(string message)
    {
        var connectionID = Context.ConnectionId;
        var userInfo = _hubState.GetUserInfo(connectionID);
        if(userInfo is null) return;

        var chatInfo = new ChatInfo()
        {
            ChatInfoID = Guid.NewGuid().ToString(),
            FromID = connectionID,
            From = userInfo.UserName,
            Message = message,
            TimeStamp = DateTime.UtcNow
        };

        // Send to everyone in the group including the caller
        await Clients.Group(userInfo.RoomName!).ReceiveMessage(chatInfo);
    }

    public async Task SendPrivateMessage(string targetConnectonID, string message)
    {
        var connectionID = Context.ConnectionId;
        var userSenderInfo = _hubState.GetUserInfo(connectionID);
        var userTargetInfo = _hubState.GetUserInfo(targetConnectonID);
        if(userSenderInfo is null) return;
        if(userTargetInfo is null) throw new HubException($"User '{targetConnectonID}' is not connected.");
        var chatInfo = new ChatInfo()
        {
            ChatInfoID = Guid.NewGuid().ToString(),
            FromID = connectionID,
            From = userSenderInfo.UserName,
            Message = message,
            TimeStamp = DateTime.UtcNow
        };

        // Send to private clinet
        await Clients.Client(targetConnectonID).ReceivePrivateMessage(chatInfo);
    }
}
