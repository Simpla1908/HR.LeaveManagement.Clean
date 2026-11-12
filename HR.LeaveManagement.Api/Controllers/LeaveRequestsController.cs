
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.CancelLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.ChangeLeaveRequestApproval;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.DeleteLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveRequest.Queries.GetLeaveRequestList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public LeaveRequestsController(ISender sender)
    {
        _sender = sender;
    }

    // GET: api/LeaveRequests
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLeaveRequestListQuery
            {
                IsLoggedInUser = !User.IsInRole("Administrator")
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetLeaveRequestListQuery { IsLoggedInUser = true }, cancellationToken));
    }

    // GET: api/LeaveRequests/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLeaveRequestDetailQuery { Id = id },
            cancellationToken);

        return Ok(result);
    }

    // POST: api/LeaveRequests
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // PUT: api/LeaveRequests/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Ids do not match.");
        }

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // PUT: api/LeaveRequests/5/cancel
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new CancelLeaveRequestCommand { Id = id },
            cancellationToken);

        return NoContent();
    }

    // PUT: api/LeaveRequests/5/approval
    [HttpPut("{id:int}/approval")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ChangeApproval(
        int id,
        [FromBody] ChangeLeaveRequestApprovalCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Ids do not match.");
        }

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // DELETE: api/LeaveRequests/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new DeleteLeaveRequestCommand { Id = id },
            cancellationToken);

        return NoContent();
    }
}
