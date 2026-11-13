using AutoMapper;
using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using MediatR;


namespace HR.LeaveManagement.Application.Features.LeaveRequest.Commands.UpdateLeaveRequest;

public sealed class UpdateLeaveRequestCommandHandler(
    ILeaveRequestRepository requests,
    ILeaveAllocationRepository allocations,
    IUserService user,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    LeaveRequestNotification notification,
    IValidator<UpdateLeaveRequestCommand> validator)
    : IRequestHandler<UpdateLeaveRequestCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // Vérifier les données avant de lancer le traitement métier.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Validation failed", validationResult.Errors);
        }

        var employeeId = await unitOfWork.ExecuteAsync(async () =>
        {
            var entity = await requests.GetByIdAsync(request.Id)
                ?? throw new NotFoundException(nameof(Domain.LeaveRequest), request.Id);
            LeaveRequestRules.EnsureAllowed(user, entity);

            if (entity.Approved.HasValue || entity.Cancelled)
            {
                throw new BadRequestException("Only pending requests can be edited.");
            }

            if (request.Cancelled)
            {
                throw new BadRequestException("Use the cancellation action to cancel a request.");
            }

            var days = LeaveRequestRules.GetDays(request.StartDate, request.EndDate);
            var allocation = await allocations.GetUserAllocations(entity.RequestingEmployeeId, request.LeaveTypeId, request.StartDate.Year)
                ?? throw new BadRequestException("No allocation exists for this leave type and year.");

            if (days > allocation.NumberOfDays)
            {
                throw new BadRequestException("Insufficient leave balance.");
            }

            mapper.Map(request, entity);
            await requests.UpdateAsync(entity);
            return entity.RequestingEmployeeId;
        }, cancellationToken);

        await notification.SendAsync(employeeId, "Leave request updated");
        return Unit.Value;
    }
}
