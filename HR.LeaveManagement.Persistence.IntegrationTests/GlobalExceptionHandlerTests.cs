using System.Text.Json;
using HR.LeaveManagement.Api.Middleware;
using HR.LeaveManagement.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData(400, "Invalid data")]
    [InlineData(403, "Access denied")]
    [InlineData(404, "Resource not found")]
    public async Task Known_errors_return_the_expected_http_response(int statusCode, string title)
    {
        Exception exception;
        if (statusCode == 400)
        {
            exception = new BadRequestException("Invalid data");
        }
        else if (statusCode == 403)
        {
            exception = new ForbiddenException();
        }
        else
        {
            exception = new NotFoundException("LeaveRequest", 123);
        }

        using var services = new ServiceCollection().AddOptions().BuildServiceProvider();
        using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        var problem = json.RootElement;
        Assert.Equal(statusCode, problem.GetProperty("status").GetInt32());
        Assert.Equal(title, problem.GetProperty("title").GetString());
        if (statusCode == 400)
        {
            Assert.Equal(JsonValueKind.Object, problem.GetProperty("errors").ValueKind);
        }
        else if (statusCode == 404)
        {
            Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
        }
    }

    [Fact]
    public async Task Unexpected_errors_are_left_to_aspnet_without_writing_a_response()
    {
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context,
            new InvalidOperationException("Technical error"), TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(0, body.Length);
    }
}
