
using FluentValidation;
using HR.LeaveManagement.Application.Contracts.Persistence;

namespace HR.LeaveManagement.Application.Features.LeaveType.Commands.CreateLeaveType;

public class CreateLeaveTypeCommandValidator
    : AbstractValidator<CreateLeaveTypeCommand>
{
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public CreateLeaveTypeCommandValidator(
        ILeaveTypeRepository leaveTypeRepository)
    {
        // Stocker le repository injecté.
        _leaveTypeRepository = leaveTypeRepository;

        // Règle 1 : le nom est obligatoire et limité à 70 caractères.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(70)
            .WithMessage("Name must not exceed 70 characters");

        // Règle 2 : le nombre de jours doit être de 1 à 99.
        RuleFor(x => x.DefaultDays)
            .GreaterThan(0)
            .WithMessage("DefaultDays must be at least 1")
            .LessThan(100)
            .WithMessage("DefaultDays must be less than 100");

        // Règle 3 : vérifier l'unicité du nom.
        RuleFor(x => x.Name)
            .MustAsync(LeaveTypeNameUnique)
            .When(x => !string.IsNullOrWhiteSpace(x.Name)
                && x.Name.Length <= 70)
            .WithMessage("Leave type already exists");
    }

    private Task<bool> LeaveTypeNameUnique(
        string name,
        CancellationToken cancellationToken)
    {
        return _leaveTypeRepository.IsLeaveTypeUnique(name);
    }
}
