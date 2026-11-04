using HelpdeskApi.Data;
using HelpdeskApi.Models;
using HelpdeskApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _repository;
    private readonly ITicketService _service;

    public TicketsController(ITicketRepository repository, ITicketService service)
    {
        _repository = repository;
        _service = service;
    }

    /// <summary>List tickets, optionally filtered by status (Open, InProgress, Resolved, Closed).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketSummary>>> GetAll([FromQuery] string? status)
    {
        if (status is not null && !TicketStatus.All.Contains(status))
            return BadRequest($"Unknown status '{status}'. Use one of: {string.Join(", ", TicketStatus.All)}.");

        return Ok(await _repository.GetAllAsync(status));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetail>> GetById(int id)
    {
        var ticket = await _repository.GetByIdAsync(id);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetail>> Create(CreateTicketRequest request)
    {
        var id = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignTicketRequest request) =>
        ToActionResult(await _service.AssignAsync(id, request.AgentId));

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, UpdateStatusRequest request) =>
        ToActionResult(await _service.ChangeStatusAsync(id, request.Status));

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, AddCommentRequest request)
    {
        if (await _repository.GetByIdAsync(id) is null) return NotFound();
        var commentId = await _repository.AddCommentAsync(id, request.AgentId, request.Body);
        return Created($"/api/tickets/{id}", new { commentId });
    }

    /// <summary>Ticket counts per status plus average resolution time (from a SQL view).</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<TicketStat>>> GetStats() =>
        Ok(await _repository.GetStatsAsync());

    private IActionResult ToActionResult(ServiceResult result) => result.Outcome switch
    {
        ServiceOutcome.Success  => NoContent(),
        ServiceOutcome.NotFound => NotFound(new ProblemDetails { Title = result.Error, Status = 404 }),
        _                       => BadRequest(new ProblemDetails { Title = result.Error, Status = 400 })
    };
}
