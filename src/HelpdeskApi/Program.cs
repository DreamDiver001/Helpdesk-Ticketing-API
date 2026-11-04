using HelpdeskApi.Data;
using HelpdeskApi.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    var problem = exception switch
    {
        SqlException { Number: 547 } => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "A referenced record (customer, category or agent) does not exist."
        },
        SqlException { Number: 2627 or 2601 } => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "A record with the same unique value already exists."
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
        }
    };

    if (problem.Status == StatusCodes.Status500InternalServerError)
        logger.LogError(exception, "Unhandled exception");
    else
        logger.LogWarning("Request failed: {Title}", problem.Title);

    context.Response.StatusCode = problem.Status!.Value;
    await context.Response.WriteAsJsonAsync(problem);
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
