using System;

namespace APIServer.Model.SignalR;

public class RoomSnapInfo
{
    public string? RoomName {get; set;}
    public List<UserInfo>? Members {get; set;}
    public int MemebrCount {get; set;}
}
