using AutoMapper;
using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Logging;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Application.Models.Email;

namespace HR.LeaveManagement.Application.Services;

public sealed class LeaveRequestWorkflow(
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations,
    IUserService user,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IEmailSender emailSender,
    IAppLogger<LeaveRequestWorkflow> logger)
{
    public async Task CreateAsync(CreateLeaveRequestCommand command, CancellationToken ct)
    {
        var employeeId = user.UserId ?? throw new ForbiddenException();
        await unitOfWork.ExecuteAsync(async () =>
        {
            await EnsureBalance(employeeId, command.LeaveTypeId, command.StartDate, command.EndDate);
            var entity = mapper.Map<Domain.LeaveRequest>(command);
            entity.RequestingEmployeeId = employeeId;
            entity.DateRequested = DateTime.UtcNow;
            await requests.CreateAsync(entity);
            return true;
        }, ct);
        await NotifyAsync(employeeId, "Leave request submitted");
    }

    public async Task UpdateAsync(UpdateLeaveRequestCommand command, CancellationToken ct)
    {
        var employeeId = await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await GetAllowed(command.Id);
            if (entity.Approved.HasValue || entity.Cancelled)
                throw new BadRequestException("Only pending requests can be edited.");
            if (command.Cancelled)
                throw new BadRequestException("Use the cancellation action to cancel a request.");
            await EnsureBalance(entity.RequestingEmployeeId, command.LeaveTypeId, command.StartDate, command.EndDate);
            mapper.Map(command, entity);
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, ct);
        await NotifyAsync(employeeId, "Leave request updated");
    }

    public async Task ChangeApprovalAsync(int id, bool approved, CancellationToken ct)
    {
        if (!user.IsAdministrator) throw new ForbiddenException();
        var employeeId = await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await GetAllowed(id);
            if (entity.Cancelled)
                throw new BadRequestException("A cancelled request cannot be approved or rejected.");
            if (entity.Approved == approved) return null;

            var days = approved ? LeaveDuration.GetDays(entity.StartDate, entity.EndDate) : entity.DeductedDays;
            if (approved || entity.Approved == true)
            {
                var allocation = await GetAllocation(entity.RequestingEmployeeId, entity.LeaveTypeId, entity.StartDate.Year);
                if (approved && allocation.NumberOfDays < days)
                    throw new BadRequestException("Insufficient leave balance.");
                allocation.NumberOfDays += approved ? -days : entity.DeductedDays;
                await allocations.UpdateAsync(allocation);
            }
            entity.DeductedDays = approved ? days : 0;
            entity.Approved = approved;
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, ct);
        if (employeeId is not null) await NotifyAsync(employeeId, "Leave request approval updated");
    }

    public async Task CancelAsync(int id, CancellationToken ct)
    {
        var employeeId = await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await GetAllowed(id);
            if (entity.Cancelled) return null;
            if (entity.Approved == true)
            {
                var allocation = await GetAllocation(entity.RequestingEmployeeId, entity.LeaveTypeId, entity.StartDate.Year);
                allocation.NumberOfDays += entity.DeductedDays;
                await allocations.UpdateAsync(allocation);
            }
            entity.DeductedDays = 0;
            entity.Cancelled = true;
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, ct);
        if (employeeId is not null) await NotifyAsync(employeeId, "Leave request cancelled");
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!user.IsAdministrator) throw new ForbiddenException();
        await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await GetAllowed(id);
            if (entity.Approved == true && !entity.Cancelled)
                throw new BadRequestException("Cancel an approved request before deleting it.");
            await requests.DeleteAsync(entity);
            return true;
        }, ct);
    }

    private async Task<Domain.LeaveRequest> GetAllowed(int id)
    {
        var entity = await requests.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Domain.LeaveRequest), id);
        LeaveRequestAccess.EnsureAllowed(user, entity);
        return entity;
    }

    private async Task<Domain.LeaveAllocation> GetAllocation(string employeeId, int typeId, int year) =>
        await allocations.GetUserAllocations(employeeId, typeId, year)
        ?? throw new BadRequestException("No allocation exists for this leave type and year.");

    private async Task EnsureBalance(string employeeId, int typeId, DateTime start, DateTime end)
    {
        var days = LeaveDuration.GetDays(start, end);
        var allocation = await GetAllocation(employeeId, typeId, start.Year);
        if (days > allocation.NumberOfDays)
            throw new BadRequestException("Insufficient leave balance.");
    }

    private async Task NotifyAsync(string employeeId, string subject)
    {
        try
        {
            var employee = await user.GetEmployee(employeeId);
            if (string.IsNullOrWhiteSpace(employee?.Email)) return;
            if (!await emailSender.SendEmail(new EmailMessage
            {
                To = employee.Email, Subject = subject,
                Body = "Your leave request has been updated. Sign in to view its details."
            }))
                logger.LogWarning("Leave notification was not sent.");
        }
        catch (Exception exception)
        {
            // The committed business transaction must survive notification failures.
            logger.LogWarning("Leave notification failed: {Message}", exception.Message);
        }
    }
}
