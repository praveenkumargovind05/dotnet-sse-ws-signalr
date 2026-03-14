using System;

namespace APIServer.Model.WS;

public class TaskDetail
{
    public string? Title {get; set;}
    public bool IsActive {get; set;}
    public DateTime CreatedOn {get; set;}
    public DateTime CompleteBy {get; set;}
}
