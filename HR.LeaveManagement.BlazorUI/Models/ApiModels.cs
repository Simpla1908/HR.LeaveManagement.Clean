using System.ComponentModel.DataAnnotations;

namespace HR.LeaveManagement.BlazorUI.Models;

public sealed class LoginModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public sealed class RegistrationModel
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required, MinLength(6)] public string UserName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
}
public sealed class CurrentUser
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public bool IsAdministrator => Roles.Contains("Administrator");
}
public sealed class LoginResponse { public string Token { get; set; } = ""; }
public sealed class LeaveTypeModel
{
    public int Id { get; set; }
    [Required, StringLength(70)] public string Name { get; set; } = "";
    [Range(1,99)] public int DefaultDays { get; set; } = 10;
}
public sealed class AllocationModel
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = "";
    public int Period { get; set; }
    public int NumberOfDays { get; set; }
    public LeaveTypeModel LeaveType { get; set; } = new();
}
public sealed class EmployeeModel
{
    public string Firstname { get; set; } = "";
    public string Lastname { get; set; } = "";
    public string Email { get; set; } = "";
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
    public string Status => Cancelled ? "Annul?e" : Approved switch { true => "Approuv?e", false => "Refus?e", _ => "En attente" };
}
public sealed class RequestFormModel
{
    public int Id { get; set; }
    [Range(1,int.MaxValue, ErrorMessage = "S?lectionnez un type de cong?.")] public int LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    [StringLength(1000)] public string RequestComments { get; set; } = "";
}
