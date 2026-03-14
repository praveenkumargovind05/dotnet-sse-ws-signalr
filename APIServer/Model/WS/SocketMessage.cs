using System;
using System.Text.Json;

namespace APIServer.Model.WS;

public class SocketMessage
{
    public string? Type {get; set;} = default!;
    public JsonElement Payload {get; set;}
}
