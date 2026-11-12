using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands;

public sealed class UpdateLeaveAllocationCommandHandler(
    ILeaveAllocationRepository allocations, IUserService user, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLeaveAllocationCommand, Unit>
{
    public Task<Unit> Handle(UpdateLeaveAllocationCommand request, CancellationToken cancellationToken)
    {
        if (!user.IsAdministrator) throw new ForbiddenException();
        return unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await allocations.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("LeaveAllocation", request.Id);
            if (entity.LeaveTypeId != request.LeaveTypeId || entity.Period != request.Period)
                throw new BadRequestException("An existing allocation cannot be moved to another leave type or year.");
            entity.NumberOfDays = request.NumberOfDays;
            await allocations.UpdateAsync(entity);
            return Unit.Value;
        }, cancellationToken);
    }
}
