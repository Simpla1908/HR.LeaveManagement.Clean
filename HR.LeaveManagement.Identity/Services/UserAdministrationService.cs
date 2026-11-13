using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Identity.DbContext;
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Identity.Services;

public sealed class UserAdministrationService(
    UserManager<ApplicationUser> users,
    HrLeaveManagementIdentityDbContext context,
    IUserService currentUser,
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations) : IUserAdministrationService
{
    public async Task<List<UserSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        var accounts = await users.Users.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
        var result = new List<UserSummary>();
        foreach (var account in accounts)
        {
            var roles = await users.GetRolesAsync(account);
            result.Add(new UserSummary
            {
                Id = account.Id, FirstName = account.FirstName, LastName = account.LastName,
                UserName = account.UserName ?? "", Email = account.Email ?? "",
                Role = roles.Contains("Administrator") ? "Administrator" : "Employee"
            });
        }
        return result;
    }

    public async Task<string> CreateAsync(CreateUserRequest request)
    {
        EnsureAdministrator();
        EnsureRole(request.Role);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var account = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = request.Email.Trim(), UserName = request.UserName.Trim()
        };
        CheckResult(await users.CreateAsync(account, request.Password));
        CheckResult(await users.AddToRoleAsync(account, request.Role));
        await transaction.CommitAsync();
        return account.Id;
    }

    public async Task UpdateAsync(string id, UserDetailsRequest request)
    {
        EnsureAdministrator();
        EnsureRole(request.Role);
        var account = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        var roles = await users.GetRolesAsync(account);
        // Les anciens comptes peuvent porter les deux rôles : choisir Employee doit retirer Administrator.
        var roleChanged = !roles.Contains(request.Role)
            || (request.Role == "Employee" && roles.Contains("Administrator"));
        if (id == currentUser.UserId && request.Role != "Administrator")
        {
            throw new BadRequestException("Vous ne pouvez pas retirer votre propre rôle administrateur.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        account.FirstName = request.FirstName.Trim();
        account.LastName = request.LastName.Trim();
        account.UserName = request.UserName.Trim();
        account.Email = request.Email.Trim();
        CheckResult(await users.UpdateAsync(account));
        if (roleChanged)
        {
            CheckResult(await users.RemoveFromRolesAsync(account, roles));
            CheckResult(await users.AddToRoleAsync(account, request.Role));
            // Un ancien jeton ne doit pas conserver les permissions du rôle retiré.
            CheckResult(await users.UpdateSecurityStampAsync(account));
        }
        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(string id)
    {
        EnsureAdministrator();
        if (id == currentUser.UserId)
        {
            throw new BadRequestException("Vous ne pouvez pas supprimer votre propre compte.");
        }
        var account = await users.FindByIdAsync(id) ?? throw new NotFoundException("User", id);
        if ((await requests.GetLeaveRequestsWithDetails(id)).Count > 0 ||
            (await allocations.GetLeaveAllocationsWithDetails(id)).Count > 0)
        {
            throw new BadRequestException("Cet utilisateur possède des demandes ou des allocations. Supprimez d'abord ces données pour conserver la cohérence de l'historique.");
        }
        CheckResult(await users.DeleteAsync(account));
    }

    private void EnsureAdministrator()
    {
        if (!currentUser.IsAdministrator) throw new ForbiddenException();
    }

    private static void EnsureRole(string role)
    {
        if (role != "Employee" && role != "Administrator")
            throw new BadRequestException("Sélectionnez un rôle valide.");
    }

    private static void CheckResult(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
