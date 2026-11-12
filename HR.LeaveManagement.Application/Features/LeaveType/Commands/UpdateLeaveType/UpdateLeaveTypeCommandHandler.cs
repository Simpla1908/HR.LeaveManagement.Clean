
using AutoMapper;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Domain;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveType.Commands.UpdateLeaveType;

public class UpdateLeaveTypeCommandHandler
    : IRequestHandler<UpdateLeaveTypeCommand, Unit>
{
    private readonly IMapper _mapper;
    private readonly ILeaveTypeRepository _repository;

    public UpdateLeaveTypeCommandHandler(
        IMapper mapper,
        ILeaveTypeRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public async Task<Unit> Handle(
        UpdateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(request.Id);

        if (leaveType is null)
        {
            throw new NotFoundException(
                nameof(LeaveType),
                request.Id);
        }

        _mapper.Map(request, leaveType);

        await _repository.UpdateAsync(leaveType);

        return Unit.Value;
    }
}
