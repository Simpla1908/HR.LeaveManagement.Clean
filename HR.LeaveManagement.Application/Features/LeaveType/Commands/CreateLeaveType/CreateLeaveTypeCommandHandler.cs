
using AutoMapper;
using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Domain;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveType.Commands.CreateLeaveType;

public class CreateLeaveTypeCommandHandler
    : IRequestHandler<CreateLeaveTypeCommand, int>
{
    private readonly IMapper _mapper;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IValidator<CreateLeaveTypeCommand> _validator;

    public CreateLeaveTypeCommandHandler(
        IMapper mapper,
        ILeaveTypeRepository leaveTypeRepository,
        IValidator<CreateLeaveTypeCommand> validator)
    {
        _mapper = mapper;
        _leaveTypeRepository = leaveTypeRepository;
        _validator = validator;
    }

    public async Task<int> Handle(
        CreateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

        var leaveType = _mapper.Map<Domain.LeaveType>(request);

        await _leaveTypeRepository.CreateAsync(leaveType);

        return leaveType.Id;
    }
}
