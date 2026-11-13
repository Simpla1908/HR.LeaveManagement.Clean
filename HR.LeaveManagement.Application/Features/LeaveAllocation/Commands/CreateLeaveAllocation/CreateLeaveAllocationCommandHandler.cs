using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using MediatR;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands;

public sealed class CreateLeaveAllocationCommandHandler(
    ILeaveAllocationRepository allocations, ILeaveTypeRepository types, IUserService user, IUnitOfWork unitOfWork,
    IValidator<CreateLeaveAllocationCommand> validator)
    : IRequestHandler<CreateLeaveAllocationCommand, Unit>
{
    public async Task<Unit> Handle(CreateLeaveAllocationCommand request, CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

        if (!user.IsAdministrator) throw new ForbiddenException();
        return await unitOfWork.ExecuteAsync(async () =>
        {
            var type = await types.GetByIdAsync(request.LeaveTypeId)
                ?? throw new NotFoundException("LeaveType", request.LeaveTypeId);
            var employees = await user.GetEmployees();
            var period = DateTime.UtcNow.Year;
            var pending = new List<Domain.LeaveAllocation>();
            foreach (var employee in employees)
            {
                if (!await allocations.AllocationExists(employee.Id, type.Id, period))
                    pending.Add(new Domain.LeaveAllocation
                    {
                        EmployeeId = employee.Id, LeaveTypeId = type.Id,
                        NumberOfDays = type.DefaultDays, Period = period
                    });
            }
            if (pending.Count > 0) await allocations.AddAllocations(pending);
            return Unit.Value;
        }, cancellationToken);
    }
}
