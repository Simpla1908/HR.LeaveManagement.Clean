using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandler(
    ILeaveRequestRepository requests,
    IUserService user,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(DeleteLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (!user.IsAdministrator)
        {
            throw new ForbiddenException();
        }

        await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await requests.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Domain.LeaveRequest), request.Id);
            LeaveRequestRules.EnsureAllowed(user, entity);

            if (entity.Approved == true && !entity.Cancelled)
            {
                throw new BadRequestException("Cancel an approved request before deleting it.");
            }

            await requests.DeleteAsync(entity);
            return true;
        }, cancellationToken);
        return Unit.Value;
    }
}
