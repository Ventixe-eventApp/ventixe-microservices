using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ventixe.AI.Service.Services;

namespace Ventixe.AI.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AIController : ControllerBase
{
    private readonly EventIndexer _indexer;
    public AIController(EventIndexer indexer)
    {
        _indexer = indexer;
    }

    [HttpPost("index")]
    public async Task<IActionResult> RunIndexer()
    {
        try
        {
            await _indexer.ProcessAndIndexEventsAsync();
            return Ok(new { message = "Indexing successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
