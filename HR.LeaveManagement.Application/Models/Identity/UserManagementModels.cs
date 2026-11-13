using System.ComponentModel.DataAnnotations;

namespace HR.LeaveManagement.Application.Models.Identity;

public class UserDetailsRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6)] public string UserName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Required, RegularExpression("^(Employee|Administrator)$")]
    public string Role { get; set; } = "Employee";
}

public class CreateUserRequest : UserDetailsRequest
{
    [Required, MinLength(8)] public string Password { get; set; } = "";
}

public class UserSummary : UserDetailsRequest
{
    public string Id { get; set; } = "";
}
