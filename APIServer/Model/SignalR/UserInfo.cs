using System;

namespace APIServer.Model.SignalR;

public class UserInfo
{
    public string? ConnectionID {get; set;}
    public string? UserName {get; set;}
    public string? RoomName {get; set;}
    public DateTime? JoinedOn {get; set;}
}
