using Microsoft.AspNetCore.Mvc;

namespace Jira_Creator
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly GeminiService _geminiService;

        public TicketController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTicket([FromBody] TicketRequest request)
        {
            //var result = await _geminiService.ListAvailableModelsAsync();
            
            var result = await _geminiService.GenerateTicketAsync(request.input);
            return Ok(new { ticket = result });
        }
    }
}
