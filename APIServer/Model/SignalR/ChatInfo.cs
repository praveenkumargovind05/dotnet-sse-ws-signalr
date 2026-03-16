using System;

namespace APIServer.Model.SignalR;

public class ChatInfo
{
    public string? ChatInfoID {get; set;}
    public string? FromID {get; set;}
    public string? From {get; set;}
    public string? Message {get; set;}
    public DateTime? TimeStamp {get; set;}
}
