using FluentValidation.Results;
using HR.LeaveManagement.Application.Exceptions;

namespace HR.LeaveManagement.Application.UnitTests;

public class ExceptionTests
{
    [Fact]
    public void Business_error_keeps_its_message_without_field_errors()
    {
        var exception = new BadRequestException("Insufficient leave balance.");

        Assert.Equal("Insufficient leave balance.", exception.Message);
        Assert.Empty(exception.ValidationErrors);
    }

    [Fact]
    public void Validation_errors_keep_all_messages_grouped_by_field()
    {
        var errors = new[]
        {
            new ValidationFailure("StartDate", "Start date is required."),
            new ValidationFailure("EndDate", "End date is required."),
            new ValidationFailure("StartDate", "Start date must be in the same year.")
        };

        var exception = new BadRequestException("Validation failed", errors);

        Assert.Equal(2, exception.ValidationErrors.Count);
        Assert.Equal(new[] { "Start date is required.", "Start date must be in the same year." },
            exception.ValidationErrors["StartDate"]);
        Assert.Equal(new[] { "End date is required." }, exception.ValidationErrors["EndDate"]);
    }
}
