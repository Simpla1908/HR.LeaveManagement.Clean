
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Domain;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveType.Commands.DeleteLeaveType;


public class DeleteLeaveTypeCommandHandler
    : IRequestHandler<DeleteLeaveTypeCommand, Unit>
{
    private readonly ILeaveTypeRepository _repository;

    public DeleteLeaveTypeCommandHandler(
        ILeaveTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(
        DeleteLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(request.Id);

        if (leaveType is null)
        {
            throw new NotFoundException(
                nameof(LeaveType),
                request.Id);
        }

        if (await _repository.IsInUseAsync(request.Id))
        {
            throw new BadRequestException("Ce type de congé est utilisé par des demandes ou des allocations et ne peut pas être supprimé.");
        }

        await _repository.DeleteAsync(leaveType);

        return Unit.Value;
    }
}
