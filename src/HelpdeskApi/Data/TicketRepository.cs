using System.Data;
using Dapper;
using HelpdeskApi.Models;

namespace HelpdeskApi.Data;

public interface ITicketRepository
{
    Task<IEnumerable<TicketSummary>> GetAllAsync(string? status);
    Task<TicketDetail?> GetByIdAsync(int ticketId);
    Task<int> CreateAsync(CreateTicketRequest request);
    Task<bool> AssignAsync(int ticketId, int agentId);
    Task<bool> UpdateStatusAsync(int ticketId, string status);
    Task<int> AddCommentAsync(int ticketId, int? agentId, string body);
    Task<IEnumerable<TicketSummary>> GetOpenByCustomerAsync(int customerId);
    Task<IEnumerable<TicketStat>> GetStatsAsync();
}

public class TicketRepository : ITicketRepository
{
    private readonly IDbConnectionFactory _factory;

    public TicketRepository(IDbConnectionFactory factory) => _factory = factory;

    private const string TicketSelect = @"
        SELECT t.TicketId, t.Title, t.Status, t.Priority, t.CreatedAt,
               c.Name AS CustomerName,
               ISNULL(a.Name, 'Unassigned') AS AgentName,
               cat.Name AS Category
        FROM Tickets t
        JOIN Customers c    ON c.CustomerId = t.CustomerId
        JOIN Categories cat ON cat.CategoryId = t.CategoryId
        LEFT JOIN Agents a  ON a.AgentId = t.AgentId";

    public async Task<IEnumerable<TicketSummary>> GetAllAsync(string? status)
    {
        using var db = _factory.Create();
        var sql = TicketSelect + " WHERE (@Status IS NULL OR t.Status = @Status) ORDER BY t.CreatedAt DESC";
        return await db.QueryAsync<TicketSummary>(sql, new { Status = status });
    }

    public async Task<TicketDetail?> GetByIdAsync(int ticketId)
    {
        using var db = _factory.Create();

        var ticket = await db.QuerySingleOrDefaultAsync<TicketDetail>(@"
            SELECT t.TicketId, t.Title, t.Description, t.Status, t.Priority,
                   t.CreatedAt, t.ResolvedAt,
                   c.Name AS CustomerName,
                   ISNULL(a.Name, 'Unassigned') AS AgentName,
                   cat.Name AS Category
            FROM Tickets t
            JOIN Customers c    ON c.CustomerId = t.CustomerId
            JOIN Categories cat ON cat.CategoryId = t.CategoryId
            LEFT JOIN Agents a  ON a.AgentId = t.AgentId
            WHERE t.TicketId = @TicketId", new { TicketId = ticketId });

        if (ticket is null) return null;

        var comments = await db.QueryAsync<TicketComment>(@"
            SELECT cm.CommentId, cm.TicketId, cm.AgentId,
                   ISNULL(a.Name, 'Customer') AS Author,
                   cm.Body, cm.CreatedAt
            FROM TicketComments cm
            LEFT JOIN Agents a ON a.AgentId = cm.AgentId
            WHERE cm.TicketId = @TicketId
            ORDER BY cm.CreatedAt", new { TicketId = ticketId });

        ticket.Comments = comments.ToList();
        return ticket;
    }

    public async Task<int> CreateAsync(CreateTicketRequest request)
    {
        using var db = _factory.Create();
        return await db.QuerySingleAsync<int>(@"
            INSERT INTO Tickets (CustomerId, CategoryId, Title, Description, Priority)
            OUTPUT INSERTED.TicketId
            VALUES (@CustomerId, @CategoryId, @Title, @Description, @Priority)", request);
    }

    public async Task<bool> AssignAsync(int ticketId, int agentId)
    {
        using var db = _factory.Create();
        var rows = await db.QuerySingleAsync<int>(
            "usp_AssignTicket",
            new { TicketId = ticketId, AgentId = agentId },
            commandType: CommandType.StoredProcedure);
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(int ticketId, string status)
    {
        using var db = _factory.Create();
        var rows = await db.QuerySingleAsync<int>(
            "usp_UpdateTicketStatus",
            new { TicketId = ticketId, Status = status },
            commandType: CommandType.StoredProcedure);
        return rows > 0;
    }

    public async Task<int> AddCommentAsync(int ticketId, int? agentId, string body)
    {
        using var db = _factory.Create();
        return await db.QuerySingleAsync<int>(
            "usp_AddComment",
            new { TicketId = ticketId, AgentId = agentId, Body = body },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<TicketSummary>> GetOpenByCustomerAsync(int customerId)
    {
        using var db = _factory.Create();
        return await db.QueryAsync<TicketSummary>(
            "usp_GetOpenTicketsByCustomer",
            new { CustomerId = customerId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<TicketStat>> GetStatsAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<TicketStat>("SELECT Status, TicketCount, AvgResolutionHours FROM vw_TicketStats");
    }
}
