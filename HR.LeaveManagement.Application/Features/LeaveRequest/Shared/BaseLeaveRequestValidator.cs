using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;

namespace HR.LeaveManagement.Application.Features.LeaveRequest.Shared
{
    public class BaseLeaveRequestValidator : AbstractValidator<BaseLeaveRequest>
    {
        private readonly ILeaveTypeRepository _leaveTypeRepository;

        public BaseLeaveRequestValidator(ILeaveTypeRepository leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
            RuleFor(p => p.StartDate).NotEmpty();
            RuleFor(p => p.EndDate).NotEmpty();
            RuleFor(p => p.StartDate)
                .Must((request, start) => start.Date <= request.EndDate.Date).WithMessage("Start date must be on or before end date.");

            RuleFor(p => p.EndDate)
                .Must((request, end) => end.Date >= request.StartDate.Date).WithMessage("End date must be on or after start date.");

            RuleFor(p => p.EndDate)
                .Must((request, end) => end.Year == request.StartDate.Year)
                .WithMessage("A request must stay within one calendar year.");

            RuleFor(p => p.LeaveTypeId)
                .GreaterThan(0)
                .MustAsync(LeaveTypeMustExist)
                .WithMessage("{PropertyName} does not exist.");
        }

        private async Task<bool> LeaveTypeMustExist(int id, CancellationToken arg2)
        {
            var leaveType = await _leaveTypeRepository.GetByIdAsync(id);
            return leaveType != null;
        }
    }
}
