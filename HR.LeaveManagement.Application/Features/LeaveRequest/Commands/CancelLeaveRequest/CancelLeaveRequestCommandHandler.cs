using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.CancelLeaveRequest;

public sealed class CancelLeaveRequestCommandHandler(
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations,
    IUserService user,
    IUnitOfWork unitOfWork,
    LeaveRequestNotification notification)
    : IRequestHandler<CancelLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await unitOfWork.ExecuteAsync<string?>(async () =>
        {
            var entity = await requests.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Domain.LeaveRequest), request.Id);
            LeaveRequestRules.EnsureAllowed(user, entity);

            // Ne pas restituer les jours une deuxième fois.
            if (entity.Cancelled)
            {
                return null;
            }

            if (entity.Approved == true)
            {
                var allocation = await allocations.GetUserAllocations(entity.RequestingEmployeeId, entity.LeaveTypeId, entity.StartDate.Year)
                    ?? throw new BadRequestException("No allocation exists for this leave type and year.");
                allocation.NumberOfDays += entity.DeductedDays;
                await allocations.UpdateAsync(allocation);
            }

            entity.DeductedDays = 0;
            entity.Cancelled = true;
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, cancellationToken);

        if (employeeId is not null)
        {
            await notification.SendAsync(employeeId, "Leave request cancelled");
        }
        return Unit.Value;
    }
}
