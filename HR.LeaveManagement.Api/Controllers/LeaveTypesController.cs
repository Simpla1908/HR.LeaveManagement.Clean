
using HR.LeaveManagement.Application.Features.LeaveType.Commands.CreateLeaveType;
using HR.LeaveManagement.Application.Features.LeaveType.Commands.DeleteLeaveType;
using HR.LeaveManagement.Application.Features.LeaveType.Commands.UpdateLeaveType;
using HR.LeaveManagement.Application.Features.LeaveType.Queries.GetAllLeaveTypes;
using HR.LeaveManagement.Application.Features.LeaveType.Queries.GetLeaveTypeDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ISender _sender;

    public LeaveTypesController(ISender sender)
    {
        _sender = sender;
    }

    // GET: api/LeaveTypes
    [HttpGet]
    [ProducesResponseType(typeof(List<LeaveTypeDto>), 200)]
    public async Task<ActionResult<List<LeaveTypeDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLeaveTypesQuery(),
            cancellationToken);

        return Ok(result);
    }

    // GET: api/LeaveTypes/5
    [HttpGet("{id:int}", Name = "GetLeaveTypeById")]
    [ProducesResponseType(typeof(LeaveTypeDetailsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LeaveTypeDetailsDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLeaveTypeDetailsQuery(id),
            cancellationToken);

        return Ok(result);
    }

    // POST: api/LeaveTypes
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLeaveTypeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtRoute(
            "GetLeaveTypeById",
            new { id },
            new { id });
    }

    // PUT: api/LeaveTypes/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLeaveTypeCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(
                "Route Id and command Id must match.");
        }

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    // DELETE: api/LeaveTypes/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteLeaveTypeCommand
        {
            Id = id
        };

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
