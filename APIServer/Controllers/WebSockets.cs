using System.Net.WebSockets;
using System.Text;
using APIServer.Services.WS.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebSockets(IChatHandler chatHandler) : ControllerBase
    {
        public readonly IChatHandler _chatHandler = chatHandler;

        [HttpGet("/chat")]
        public async Task<IActionResult> InitiateChat(CancellationToken cancellationToken)
        {
            if(!HttpContext.WebSockets.IsWebSocketRequest)
                return BadRequest();

            var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _chatHandler.HandleAsync(ws, cancellationToken);

            return Ok();
        }
    }
}
