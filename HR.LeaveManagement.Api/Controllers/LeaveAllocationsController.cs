
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocationDetails;
using HR.LeaveManagement.Application.Features.LeaveAllocation.Queries.GetLeaveAllocations;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveAllocationsController : ControllerBase
{
    private readonly ISender _sender;

    public LeaveAllocationsController(ISender sender)
    {
        _sender = sender;
    }

    // GET: api/LeaveAllocations
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<List<LeaveAllocationDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLeaveAllocationListQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetLeaveAllocationListQuery { IsLoggedInUser = true }, cancellationToken));
    }

    // GET: api/LeaveAllocations/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<LeaveAllocationDetailsDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var query = new GetLeaveAllocationDetailQuery
        {
            Id = id
        };

        var result = await _sender.Send(
            query,
            cancellationToken);

        return Ok(result);
    }

    // POST: api/LeaveAllocations
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateLeaveAllocationCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // PUT: api/LeaveAllocations/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLeaveAllocationCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Ids do not match.");
        }

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // DELETE: api/LeaveAllocations/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteLeaveAllocationCommand
        {
            Id = id
        };

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
