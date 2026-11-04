using HelpdeskApi.Data;
using HelpdeskApi.Models;
using HelpdeskApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HelpdeskApi.Tests;

public class TicketWorkflowTests
{
    [Theory]
    [InlineData("Open", "InProgress", true)]
    [InlineData("Open", "Closed", true)]
    [InlineData("Open", "Resolved", false)]   
    [InlineData("InProgress", "Resolved", true)]
    [InlineData("InProgress", "Open", true)]
    [InlineData("Resolved", "InProgress", true)] 
    [InlineData("Resolved", "Closed", true)]
    [InlineData("Closed", "Open", false)]        
    [InlineData("Closed", "InProgress", false)]
    public void CanTransition_FollowsLifecycleRules(string from, string to, bool expected)
    {
        Assert.Equal(expected, TicketWorkflow.CanTransition(from, to));
    }

    [Fact]
    public void CanTransition_UnknownStatus_ReturnsFalse()
    {
        Assert.False(TicketWorkflow.CanTransition("Banana", "Open"));
    }
}

public class TicketServiceTests
{
    private static TicketDetail TicketWithStatus(string status) =>
        new() { TicketId = 1, Title = "Test ticket", Status = status };

    private static TicketService CreateService(Mock<ITicketRepository> repo) =>
        new(repo.Object, NullLogger<TicketService>.Instance);

    [Fact]
    public async Task ChangeStatus_TicketMissing_ReturnsNotFound()
    {
        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TicketDetail?)null);

        var result = await CreateService(repo).ChangeStatusAsync(99, "InProgress");

        Assert.Equal(ServiceOutcome.NotFound, result.Outcome);
        repo.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_IsRejectedAndNotSaved()
    {
        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TicketWithStatus("Closed"));

        var result = await CreateService(repo).ChangeStatusAsync(1, "Open");

        Assert.Equal(ServiceOutcome.Invalid, result.Outcome);
        repo.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_ValidTransition_SavesNewStatus()
    {
        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TicketWithStatus("Open"));
        repo.Setup(r => r.UpdateStatusAsync(1, "InProgress")).ReturnsAsync(true);

        var result = await CreateService(repo).ChangeStatusAsync(1, "InProgress");

        Assert.Equal(ServiceOutcome.Success, result.Outcome);
        repo.Verify(r => r.UpdateStatusAsync(1, "InProgress"), Times.Once);
    }

    [Fact]
    public async Task Assign_TicketNotOpen_IsRejected()
    {
        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TicketWithStatus("Resolved"));

        var result = await CreateService(repo).AssignAsync(1, agentId: 2);

        Assert.Equal(ServiceOutcome.Invalid, result.Outcome);
        repo.Verify(r => r.AssignAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Assign_OpenTicket_CallsRepository()
    {
        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TicketWithStatus("Open"));
        repo.Setup(r => r.AssignAsync(1, 2)).ReturnsAsync(true);

        var result = await CreateService(repo).AssignAsync(1, agentId: 2);

        Assert.Equal(ServiceOutcome.Success, result.Outcome);
    }
}
