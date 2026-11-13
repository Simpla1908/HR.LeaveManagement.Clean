using System.ComponentModel.DataAnnotations;

namespace HR.LeaveManagement.BlazorUI.Models;

public sealed class LoginModel
{
    [Required(ErrorMessage = "Saisissez votre adresse e-mail."), EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Saisissez votre mot de passe.")]
    public string Password { get; set; } = "";
}
public sealed class RegistrationModel
{
    [Required(ErrorMessage = "Le prénom est obligatoire.")] public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Le nom est obligatoire.")] public string LastName { get; set; } = "";
    [Required, MinLength(6, ErrorMessage = "L'identifiant doit contenir au moins 6 caractères.")] public string UserName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
}
public sealed class CurrentUser
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public bool IsAdministrator => Roles.Contains("Administrator");
}
public sealed class LoginResponse { public string Token { get; set; } = ""; }
public sealed class LeaveTypeModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Le nom est obligatoire."), StringLength(70, ErrorMessage = "70 caractères maximum.")]
    public string Name { get; set; } = "";
    [Range(1, 99, ErrorMessage = "Le quota doit être compris entre 1 et 99 jours.")]
    public int DefaultDays { get; set; } = 20;
}
public sealed class AllocationModel
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = "";
    public int LeaveTypeId { get; set; }
    public int Period { get; set; }
    public int NumberOfDays { get; set; }
    public LeaveTypeModel LeaveType { get; set; } = new();
}
public sealed class AllocationFormModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Sélectionnez un type de congé.")]
    public int LeaveTypeId { get; set; }
    public int Period { get; set; } = DateTime.Today.Year;
    [Range(0, int.MaxValue, ErrorMessage = "Le solde ne peut pas être négatif.")]
    public int NumberOfDays { get; set; }
}
public sealed class EmployeeModel
{
    public string Firstname { get; set; } = "";
    public string Lastname { get; set; } = "";
    public string Email { get; set; } = "";
}
public sealed class UserModel
{
    public string Id { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "Employee";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string RoleLabel => Role == "Administrator" ? "Administrateur" : "Employé";
    public string Initials => string.Concat(FirstName.Take(1)) + string.Concat(LastName.Take(1));
}
public sealed class UserFormModel : IValidatableObject
{
    public string Id { get; set; } = "";
    [Required(ErrorMessage = "Le prénom est obligatoire."), StringLength(100)] public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Le nom est obligatoire."), StringLength(100)] public string LastName { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "L'identifiant doit contenir entre 6 et 100 caractères.")]
    public string UserName { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Adresse e-mail invalide."), StringLength(256)] public string Email { get; set; } = "";
    [Required, RegularExpression("^(Employee|Administrator)$")] public string Role { get; set; } = "Employee";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id.Length > 0) yield break;
        if (Password.Length < 8 || !Password.Any(char.IsUpper) || !Password.Any(char.IsLower)
            || !Password.Any(char.IsDigit) || !Password.Any(c => !char.IsLetterOrDigit(c)))
            yield return new ValidationResult("Le mot de passe doit contenir au moins 8 caractères, une majuscule, une minuscule, un chiffre et un symbole.", [nameof(Password)]);
        if (Password != ConfirmPassword)
            yield return new ValidationResult("Les deux mots de passe doivent être identiques.", [nameof(ConfirmPassword)]);
    }
}
public sealed class LeaveRequestModel
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveTypeModel LeaveType { get; set; } = new();
    public EmployeeModel? Employee { get; set; }
    public string RequestingEmployeeId { get; set; } = "";
    public string RequestComments { get; set; } = "";
    public bool? Approved { get; set; }
    public bool Cancelled { get; set; }
    public string Status => Cancelled ? "Annulée" : Approved switch { true => "Approuvée", false => "Refusée", _ => "En attente" };
    public string StatusClass => Cancelled ? "neutral" : Approved switch { true => "positive", false => "negative", _ => "pending" };
}
public sealed class RequestFormModel : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Sélectionnez un type de congé.")] public int LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    [StringLength(1000, ErrorMessage = "1 000 caractères maximum.")] public string RequestComments { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.Date < StartDate.Date)
            yield return new ValidationResult("La date de fin doit suivre la date de début.", [nameof(EndDate)]);
        if (StartDate.Year != EndDate.Year)
            yield return new ValidationResult("Une demande doit rester dans la même année.", [nameof(EndDate)]);
    }
}