using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands;

public sealed class DeleteLeaveAllocationCommandHandler(
    ILeaveAllocationRepository allocations, ILeaveRequestRepository requests, IUserService user, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLeaveAllocationCommand, Unit>
{
    public Task<Unit> Handle(DeleteLeaveAllocationCommand request, CancellationToken cancellationToken)
    {
        if (!user.IsAdministrator) throw new ForbiddenException();
        return unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await allocations.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("LeaveAllocation", request.Id);
            var employeeRequests = await requests.GetLeaveRequestsWithDetails(entity.EmployeeId);
            if (employeeRequests.Any(x => x.LeaveTypeId == entity.LeaveTypeId && x.StartDate.Year == entity.Period &&
                    x.Approved == true && !x.Cancelled))
                throw new BadRequestException("Cancel approved requests before deleting their allocation.");
            await allocations.DeleteAsync(entity);
            return Unit.Value;
        }, cancellationToken);
    }
}
