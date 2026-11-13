using AutoMapper;
using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using MediatR;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Commands;

public sealed class CreateLeaveRequestCommandHandler(
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations,
    IUserService user,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    LeaveRequestNotification notification,
    IValidator<CreateLeaveRequestCommand> validator)
    : IRequestHandler<CreateLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

        var employeeId = user.UserId ?? throw new ForbiddenException();

        // Vérifier le solde et enregistrer la demande dans la même transaction.
        await unitOfWork.ExecuteAsync(async () =>
        {
            var days = LeaveRequestRules.GetDays(request.StartDate, request.EndDate);
            var allocation = await allocations.GetUserAllocations(employeeId, request.LeaveTypeId, request.StartDate.Year)
                ?? throw new BadRequestException("No allocation exists for this leave type and year.");

            if (days > allocation.NumberOfDays)
            {
                throw new BadRequestException("Insufficient leave balance.");
            }

            var entity = mapper.Map<Domain.LeaveRequest>(request);
            entity.RequestingEmployeeId = employeeId;
            entity.DateRequested = DateTime.UtcNow;
            await requests.CreateAsync(entity);
            return true;
        }, cancellationToken);

        await notification.SendAsync(employeeId, "Leave request submitted");
        return Unit.Value;
    }
}
