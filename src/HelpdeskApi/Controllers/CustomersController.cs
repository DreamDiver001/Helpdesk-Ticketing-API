using HelpdeskApi.Data;
using HelpdeskApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;

    public CustomersController(ICustomerRepository customers, ITicketRepository tickets)
    {
        _customers = customers;
        _tickets = tickets;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll() =>
        Ok(await _customers.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
    {
        var id = await _customers.CreateAsync(request);
        return Created($"/api/customers/{id}", new { customerId = id });
    }

    /// <summary>Open tickets for one customer (calls the usp_GetOpenTicketsByCustomer stored procedure).</summary>
    [HttpGet("{id:int}/tickets/open")]
    public async Task<ActionResult<IEnumerable<TicketSummary>>> GetOpenTickets(int id) =>
        Ok(await _tickets.GetOpenByCustomerAsync(id));
}
