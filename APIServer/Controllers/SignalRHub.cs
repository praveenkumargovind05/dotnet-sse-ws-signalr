using APIServer.Services.SignalR.Concrete;
using APIServer.Services.SignalR.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace APIServer.Controllers
{

    /// <summary>
    /// We can access hub and send messages to connectd clients from contorller also
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="hubState"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class SignalRHub(IHubContext<ChatHub, IChatHub> hubContext, IHubState hubState) : ControllerBase
    {
        public required IHubContext<ChatHub, IChatHub> _hubContext = hubContext;
        public required IHubState _hubState = hubState;

        [HttpGet("/send-active-users")]
        public async Task<IActionResult> SendAllActiveUsers()
        {
            var users = _hubState.GetAllUsers() ?? [];
            await _hubContext.Clients.All.OnlineUsers(users);
            return Ok();
        }

        [HttpPost("/send-users-in-group")]
        public async Task<IActionResult> SendUsersInGroup([FromBody]string roomName)
        {
            var users = _hubState.GetUsersInRoom(roomName) ?? [];
            await _hubContext.Clients.Group(roomName).OnlineUsersInGroup(users);
            return Ok();
        }

        [HttpPost("/notify-all")]
        public async Task<IActionResult> NotifyAll([FromBody]string message)
        {
            await _hubContext.Clients.All.ReceiveNotification(message);
            return Ok();
        }

        [HttpPost("/notify-all-group")]
        public async Task<IActionResult> NotifyAllInGroup(string roomName, string message)
        {
            await _hubContext.Clients.Group(roomName).ReceiveNotification(message);
            return Ok();
        }

        [HttpPost("/notify-user")]
        public async Task<IActionResult> NotifyUser(string connectionID, string message)
        {
            await _hubContext.Clients.Client(connectionID).ReceiveNotification(message);
            return Ok();
        }
    }

}
