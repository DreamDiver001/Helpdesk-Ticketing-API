using HelpdeskApi.Data;
using HelpdeskApi.Models;

namespace HelpdeskApi.Services;

/// <summary>
/// Business rules for ticket lifecycle. Pure logic, no database, so it is easy to unit test.
/// </summary>
public static class TicketWorkflow
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [TicketStatus.Open]       = new[] { TicketStatus.InProgress, TicketStatus.Closed },
        [TicketStatus.InProgress] = new[] { TicketStatus.Open, TicketStatus.Resolved },
        [TicketStatus.Resolved]   = new[] { TicketStatus.InProgress, TicketStatus.Closed },
        [TicketStatus.Closed]     = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}

public enum ServiceOutcome { Success, NotFound, Invalid }

public record ServiceResult(ServiceOutcome Outcome, string? Error = null)
{
    public static ServiceResult Ok() => new(ServiceOutcome.Success);
    public static ServiceResult NotFound(string message) => new(ServiceOutcome.NotFound, message);
    public static ServiceResult Invalid(string message) => new(ServiceOutcome.Invalid, message);
}

public interface ITicketService
{
    Task<ServiceResult> ChangeStatusAsync(int ticketId, string newStatus);
    Task<ServiceResult> AssignAsync(int ticketId, int agentId);
}

public class TicketService : ITicketService
{
    private readonly ITicketRepository _repository;
    private readonly ILogger<TicketService> _logger;

    public TicketService(ITicketRepository repository, ILogger<TicketService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult> ChangeStatusAsync(int ticketId, string newStatus)
    {
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult.NotFound($"Ticket {ticketId} was not found.");

        if (!TicketWorkflow.CanTransition(ticket.Status, newStatus))
        {
            _logger.LogWarning("Rejected status change on ticket {TicketId}: {From} -> {To}",
                ticketId, ticket.Status, newStatus);
            return ServiceResult.Invalid($"A ticket cannot move from {ticket.Status} to {newStatus}.");
        }

        await _repository.UpdateStatusAsync(ticketId, newStatus);
        _logger.LogInformation("Ticket {TicketId} moved {From} -> {To}", ticketId, ticket.Status, newStatus);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AssignAsync(int ticketId, int agentId)
    {
        var ticket = await _repository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult.NotFound($"Ticket {ticketId} was not found.");

        // usp_AssignTicket only updates tickets that are still Open.
        if (ticket.Status != TicketStatus.Open)
            return ServiceResult.Invalid($"Only Open tickets can be assigned (current status: {ticket.Status}).");

        var assigned = await _repository.AssignAsync(ticketId, agentId);
        return assigned ? ServiceResult.Ok() : ServiceResult.Invalid("The ticket could not be assigned.");
    }
}
