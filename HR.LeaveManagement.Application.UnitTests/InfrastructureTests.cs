using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Logging;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
using HR.LeaveManagement.Application.Models.Email;
using HR.LeaveManagement.Application.Models.Identity;
using HR.LeaveManagement.Infrastructure;
using HR.LeaveManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SendGrid;

namespace HR.LeaveManagement.Application.UnitTests;

[CollectionDefinition("Infrastructure console", DisableParallelization = true)]
public sealed class InfrastructureConsoleCollection;

[Collection("Infrastructure console")]
public class InfrastructureTests
{
    [Theory]
    [InlineData("Console", typeof(ConsoleEmailSender))]
    [InlineData("smtp", typeof(SmtpEmailSender))]
    [InlineData("SendGrid", typeof(SendGridEmailSender))]
    public void Configuration_selects_the_email_provider(string name, Type expected)
    {
        using var provider = BuildProvider(new()
        {
            ["EmailSettings:Enabled"] = "true",
            ["EmailSettings:Provider"] = name,
            ["EmailSettings:FromAddress"] = "hr@example.test",
            ["EmailSettings:Host"] = "localhost",
            ["EmailSettings:SendGridApiKey"] = "test-key-never-sent"
        });

        Assert.IsType(expected, provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public async Task Disabled_email_requires_no_credentials_and_sends_nothing()
    {
        using var provider = BuildProvider(new() { ["EmailSettings:Enabled"] = "false" });
        var sender = provider.GetRequiredService<IEmailSender>();
        Assert.IsType<DisabledEmailSender>(sender);
        Assert.False(await sender.SendEmail(Message()));
    }

    [Theory]
    [InlineData("Unknown", "hr@example.test", "localhost", "587", "key")]
    [InlineData("Console", "invalid", "", "587", "")]
    [InlineData("Smtp", "hr@example.test", "", "587", "")]
    [InlineData("Smtp", "hr@example.test", "localhost", "0", "")]
    [InlineData("Smtp", "hr@example.test", "localhost", "65536", "")]
    [InlineData("SendGrid", "hr@example.test", "", "587", "")]
    public void Invalid_email_configuration_is_rejected(string name, string from, string host, string port, string key)
    {
        using var provider = BuildProvider(new()
        {
            ["EmailSettings:Enabled"] = "true", ["EmailSettings:Provider"] = name,
            ["EmailSettings:FromAddress"] = from, ["EmailSettings:Host"] = host,
            ["EmailSettings:Port"] = port, ["EmailSettings:SendGridApiKey"] = key
        });
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public async Task Console_backend_displays_the_message_without_network_or_credentials()
    {
        using var provider = BuildProvider(new()
        {
            ["EmailSettings:Enabled"] = "true", ["EmailSettings:Provider"] = "Console",
            ["EmailSettings:FromAddress"] = "hr@example.test"
        });
        using var output = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(output);
            Assert.True(await provider.GetRequiredService<IEmailSender>().SendEmail(Message()));
        }
        finally
        {
            Console.SetOut(original);
        }
        Assert.Contains("To: employee@example.test", output.ToString());
        Assert.Contains("Subject: Local notification", output.ToString());
        Assert.Contains("A local notification", output.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task SendGrid_sends_plain_text_and_reports_acceptance_or_failure(HttpStatusCode status, bool expected)
    {
        using var handler = new SendGridHandler(status);
        using var http = new HttpClient(handler);
        var logger = new ListLogger<SendGridEmailSender>();
        var sender = new SendGridEmailSender(new SendGridClient(http, "test-key-never-sent"),
            Options.Create(new EmailSettings { FromAddress = "hr@example.test", FromName = "HR" }), logger);

        Assert.Equal(expected, await sender.SendEmail(Message()));
        Assert.Equal("https://api.sendgrid.com/v3/mail/send", handler.Url);
        Assert.Equal("Bearer test-key-never-sent", handler.Authorization);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("hr@example.test", json.RootElement.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("employee@example.test", json.RootElement.GetProperty("personalizations")[0]
            .GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Local notification", json.RootElement.GetProperty("personalizations")[0].GetProperty("subject").GetString());
        Assert.Equal("text/plain", json.RootElement.GetProperty("content")[0].GetProperty("type").GetString());
        Assert.Equal("A local notification", json.RootElement.GetProperty("content")[0].GetProperty("value").GetString());
        Assert.DoesNotContain("private-provider-response", string.Join(" ", logger.Messages));
        Assert.DoesNotContain("test-key-never-sent", string.Join(" ", logger.Messages));
        if (!expected) Assert.Contains(((int)status).ToString(), Assert.Single(logger.Messages));
    }

    [Fact]
    public async Task Smtp_backend_delivers_to_a_loopback_server_without_tls_or_authentication()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var received = ReceiveSmtpMessage(listener, timeout.Token);
        var sender = new SmtpEmailSender(Options.Create(new EmailSettings
        {
            Enabled = true, Host = "127.0.0.1", Port = ((IPEndPoint)listener.LocalEndpoint).Port,
            EnableSsl = false, FromAddress = "hr@example.test"
        }), NullLogger<SmtpEmailSender>.Instance);

        Assert.True(await sender.SendEmail(Message()));
        var message = await received;
        Assert.Contains("employee@example.test", message);
        Assert.Contains("Subject: Local notification", message);
        Assert.Contains("A local notification", message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Notification_failure_does_not_fail_the_completed_business_operation(bool throws)
    {
        var logger = new NotificationLogger();
        var notification = new LeaveRequestNotification(new TestUser("employee@example.test"), new FailedSender(throws), logger);
        await notification.SendAsync("employee-id", "Leave request submitted");
        if (throws)
        {
            Assert.IsType<HttpRequestException>(logger.Exception);
            Assert.Contains("employee-id", logger.Arguments);
        }
        else Assert.True(logger.Warned);
    }

    [Fact]
    public async Task Missing_employee_address_skips_delivery()
    {
        var sender = new FailedSender(true);
        var logger = new NotificationLogger();
        await new LeaveRequestNotification(new TestUser(""), sender, logger).SendAsync("employee-id", "Update");
        Assert.False(sender.Called);
        Assert.True(logger.Warned);
    }

    [Fact]
    public void Serilog_preserves_properties_scopes_and_exception_details_in_a_file_under_the_content_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "hr-infrastructure-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["LogFiles:Path"] = "Logs/check-.json" }).Build();
            var services = new ServiceCollection().AddLogging();
            services.AddInfrastructureLogging(configuration, new TestEnvironment(root));
            services.AddInfrastructureServices(configuration);
            using (var provider = services.BuildServiceProvider())
            {
                using var scope = provider.GetRequiredService<ILogger<InfrastructureTests>>()
                    .BeginScope(new Dictionary<string, object> { ["RequestId"] = "request-123" });
                provider.GetRequiredService<IAppLogger<InfrastructureTests>>().LogError(
                    new InvalidOperationException("test failure"), "Notification for {EmployeeId} failed", "employee-123");
            }
            using var json = JsonDocument.Parse(File.ReadAllText(Assert.Single(Directory.GetFiles(Path.Combine(root, "Logs")))));
            Assert.Equal("Error", json.RootElement.GetProperty("Level").GetString());
            Assert.Contains("test failure", json.RootElement.GetProperty("Exception").GetString());
            var properties = json.RootElement.GetProperty("Properties");
            Assert.Equal("employee-123", properties.GetProperty("EmployeeId").GetString());
            Assert.Equal("request-123", properties.GetProperty("RequestId").GetString());
            Assert.Equal("Test", properties.GetProperty("Environment").GetString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings) =>
        new ServiceCollection().AddLogging().AddInfrastructureServices(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()).BuildServiceProvider();

    private static EmailMessage Message() => new()
    { To = "employee@example.test", Subject = "Local notification", Body = "A local notification" };

    private static async Task<string> ReceiveSmtpMessage(TcpListener listener, CancellationToken cancellationToken)
    {
        using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };
        await writer.WriteLineAsync("220 localhost test SMTP");
        var data = new StringBuilder();
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("EHLO") || line.StartsWith("HELO")) await writer.WriteLineAsync("250 localhost");
            else if (line == "DATA")
            {
                await writer.WriteLineAsync("354 End with a dot");
                while (await reader.ReadLineAsync(cancellationToken) is { } body && body != ".") data.AppendLine(body);
                await writer.WriteLineAsync("250 Message accepted");
            }
            else if (line == "QUIT")
            {
                await writer.WriteLineAsync("221 Bye");
                break;
            }
            else await writer.WriteLineAsync("250 OK");
        }
        return data.ToString();
    }

    private sealed class SendGridHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("private-provider-response") };
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class TestUser(string address) : IUserService
    {
        public string? UserId => "employee-id";
        public bool IsAdministrator => false;
        public Task<List<Employee>> GetEmployees() => Task.FromResult(new List<Employee>());
        public Task<Employee?> GetEmployee(string userId) => Task.FromResult<Employee?>(new Employee { Email = address });
    }

    private sealed class FailedSender(bool throws) : IEmailSender
    {
        public bool Called { get; private set; }
        public Task<bool> SendEmail(EmailMessage email)
        {
            Called = true;
            return throws ? throw new HttpRequestException("Email service unavailable") : Task.FromResult(false);
        }
    }

    private sealed class NotificationLogger : IAppLogger<LeaveRequestNotification>
    {
        public bool Warned { get; private set; }
        public Exception? Exception { get; private set; }
        public object[] Arguments { get; private set; } = [];
        public void LogInformation(string message, params object[] args) { }
        public void LogWarning(string message, params object[] args) => Warned = true;
        public void LogError(Exception exception, string message, params object[] args) { Exception = exception; Arguments = args; }
    }

    private sealed class TestEnvironment(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "InfrastructureTests";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
