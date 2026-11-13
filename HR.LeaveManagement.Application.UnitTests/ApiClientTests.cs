using System.Net;
using System.Text;
using HR.LeaveManagement.BlazorUI.Models;
using HR.LeaveManagement.BlazorUI.Services;

namespace HR.LeaveManagement.Application.UnitTests;

public class ApiClientTests
{
    [Fact]
    public async Task Blazor_authentication_state_follows_login_and_logout()
    {
        var client = new LeaveApiClient(new TestFactory(new RecordingHandler()));
        using var provider = new ApiAuthenticationStateProvider(client);
        Assert.False((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        var changes = 0;
        provider.AuthenticationStateChanged += _ => changes++;

        await client.LoginAsync(new LoginModel());
        var authenticated = (await provider.GetAuthenticationStateAsync()).User;
        Assert.True(authenticated.Identity!.IsAuthenticated);
        Assert.True(authenticated.IsInRole("Employee"));
        Assert.False(authenticated.IsInRole("Administrator"));
        client.Logout();
        Assert.False((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        Assert.True(changes >= 2);
    }

    [Fact]
    public async Task Tokens_are_scoped_to_each_client_and_removed_on_logout()
    {
        var handler = new RecordingHandler();
        var factory = new TestFactory(handler);
        var first = new LeaveApiClient(factory);
        var second = new LeaveApiClient(factory);
        await first.LoginAsync(new LoginModel { Email = "user@example.test", Password = "secret" });
        await first.GetTypesAsync();
        Assert.Equal("Bearer test-token", handler.LastAuthorization);
        await second.GetTypesAsync();
        Assert.Null(handler.LastAuthorization);
        first.Logout();
        await first.GetTypesAsync();
        Assert.Null(handler.LastAuthorization);
        Assert.False(first.IsSignedIn);
    }

    [Fact]
    public async Task Expired_token_clears_user_session()
    {
        var handler = new RecordingHandler();
        var client = new LeaveApiClient(new TestFactory(handler));
        await client.LoginAsync(new LoginModel());
        handler.Reject = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetTypesAsync());
        Assert.False(client.IsSignedIn);
    }

    private sealed class TestFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.example.test/") };
    }
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? LastAuthorization { get; private set; }
        public bool Reject { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastAuthorization = request.Headers.Authorization?.ToString();
            var json = request.RequestUri!.AbsolutePath switch
            {
                "/api/auth/login" => "{\"token\":\"test-token\"}",
                "/api/auth/me" => "{\"userId\":\"employee\",\"userName\":\"employee\",\"roles\":[\"Employee\"]}",
                _ => "[]"
            };
            return Task.FromResult(new HttpResponseMessage(Reject ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
