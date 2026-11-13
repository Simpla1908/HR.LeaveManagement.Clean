using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.ChangeLeaveRequestApproval;

public sealed class ChangeLeaveRequestApprovalCommandHandler(
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations,
    IUserService user,
    IUnitOfWork unitOfWork,
    LeaveRequestNotification notification,
    IValidator<ChangeLeaveRequestApprovalCommand> validator)
    : IRequestHandler<ChangeLeaveRequestApprovalCommand, Unit>
{
    public async Task<Unit> Handle(ChangeLeaveRequestApprovalCommand request, CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

        if (!user.IsAdministrator)
        {
            throw new ForbiddenException();
        }

        var employeeId = await unitOfWork.ExecuteAsync<string?>(async () =>
        {
            var entity = await requests.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Domain.LeaveRequest), request.Id);
            LeaveRequestRules.EnsureAllowed(user, entity);

            if (entity.Cancelled)
            {
                throw new BadRequestException("A cancelled request cannot be approved or rejected.");
            }

            // Une décision identique ne doit pas débiter ou restituer les jours à nouveau.
            if (entity.Approved == request.Approved)
            {
                return null;
            }

            var days = 0;
            if (request.Approved)
            {
                days = LeaveRequestRules.GetDays(entity.StartDate, entity.EndDate);
            }

            if (request.Approved || entity.Approved == true)
            {
                var allocation = await allocations.GetUserAllocations(entity.RequestingEmployeeId, entity.LeaveTypeId, entity.StartDate.Year)
                    ?? throw new BadRequestException("No allocation exists for this leave type and year.");

                if (request.Approved)
                {
                    if (allocation.NumberOfDays < days)
                    {
                        throw new BadRequestException("Insufficient leave balance.");
                    }

                    allocation.NumberOfDays -= days;
                }
                else
                {
                    // Le rejet d'une demande approuvée restitue son débit exact.
                    allocation.NumberOfDays += entity.DeductedDays;
                }

                await allocations.UpdateAsync(allocation);
            }

            entity.DeductedDays = days;
            entity.Approved = request.Approved;
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, cancellationToken);

        if (employeeId is not null)
        {
            await notification.SendAsync(employeeId, "Leave request approval updated");
        }
        return Unit.Value;
    }
}
