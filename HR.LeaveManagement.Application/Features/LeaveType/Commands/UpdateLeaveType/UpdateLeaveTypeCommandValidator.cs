
using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Persistence;

namespace HR.LeaveManagement.Application.Features.LeaveType.Commands.UpdateLeaveType;

public class UpdateLeaveTypeCommandValidator
    : AbstractValidator<UpdateLeaveTypeCommand>
{
    private readonly ILeaveTypeRepository _repository;

    public UpdateLeaveTypeCommandValidator(
        ILeaveTypeRepository repository)
    {
        _repository = repository;

        // Vérifier que l'identifiant est positif.
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0");

        // Vérifier que le type de congé existe.
        RuleFor(x => x.Id)
            .MustAsync(LeaveTypeMustExist)
            .When(x => x.Id > 0)
            .WithMessage("Leave type does not exist");

        // Vérifier le nom.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(70)
            .WithMessage("Name must not exceed 70 characters");

        // Vérifier le nombre de jours.
        RuleFor(x => x.DefaultDays)
            .InclusiveBetween(1, 99)
            .WithMessage("DefaultDays must be between 1 and 99");
    }

    private async Task<bool> LeaveTypeMustExist(
        int id,
        CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(id);

        return leaveType is not null;
    }
}
