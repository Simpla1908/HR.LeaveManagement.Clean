
using AutoMapper;
using FluentValidation;
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
    private readonly IValidator<UpdateLeaveTypeCommand> _validator;

    public UpdateLeaveTypeCommandHandler(
        IMapper mapper,
        ILeaveTypeRepository repository,
        IValidator<UpdateLeaveTypeCommand> validator)
    {
        _mapper = mapper;
        _repository = repository;
        _validator = validator;
    }

    public async Task<Unit> Handle(
        UpdateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

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
