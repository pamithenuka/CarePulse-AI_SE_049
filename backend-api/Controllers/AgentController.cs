using CarePulse.Api.DTOs;
using CarePulse.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/v1/agent")]
public class AgentController : ControllerBase
{
    private readonly GeminiAgentService _agent;

    public AgentController(GeminiAgentService agent)
    {
        _agent = agent;
    }

    // POST /api/v1/agent/search
    // Agent 3 (Action / Tool Agent). Takes a plain-English message from a
    // patient and returns a natural-language reply plus the real matching
    // open slots it found via a live, read-only database search.
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] AgentSearchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        try
        {
            var result = await _agent.HandleMessageAsync(request.Message);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Covers missing/invalid API key and Gemini API errors -
            // surfaced clearly instead of a raw 500.
            return StatusCode(502, new { Message = ex.Message });
        }
    }
}
