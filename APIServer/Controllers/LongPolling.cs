using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LongPolling : ControllerBase
    {
        [HttpGet("/get-new-record")]
        public async Task<IActionResult> GetNewRecord(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                // Implementation 1 - No Loop, Minimal CPU overhead
                var timeOutDelay = Task.Delay(-1, cts.Token);
                var itemArrivedTask = WaitForNewItem(cts.Token);

                var completedTask = await Task.WhenAny(timeOutDelay, itemArrivedTask);
                if(completedTask == itemArrivedTask)
                    return Ok(itemArrivedTask.Result);

                return NoContent();
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/get-new-item")]
        public async Task<IActionResult> GetNewItem(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                // Implementation 2 - Not ideal [10,000 clients, 10,000 loops]
                while(!cts.IsCancellationRequested)
                {
                    var newItem = await WaitForNewItem(cts.Token);
                    if(!string.IsNullOrEmpty(newItem))
                        return Ok(newItem);
                    await Task.Delay(10, cts.Token);
                }
                return NoContent();
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        } 

        [NonAction]
        private static async Task<string> WaitForNewItem(CancellationToken token)
        {
            var randomSec = Random.Shared.Next(1, 40);
            await Task.Delay(TimeSpan.FromSeconds(randomSec), token);
            return "Hello";
        }
    }
}
