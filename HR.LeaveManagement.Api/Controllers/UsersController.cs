using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR.LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrator")]
public class UsersController(IUserAdministrationService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserSummary>>> GetAll(CancellationToken cancellationToken)
        => Ok(await users.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
        => StatusCode(StatusCodes.Status201Created, new { id = await users.CreateAsync(request) });

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UserDetailsRequest request)
    {
        await users.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await users.DeleteAsync(id);
        return NoContent();
    }
}
