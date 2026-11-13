using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HR.LeaveManagement.BlazorUI.Services;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly LeaveApiClient api;
    public ApiAuthenticationStateProvider(LeaveApiClient api)
    {
        this.api = api;
        api.SessionChanged += OnSessionChanged;
    }
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        if (api.User is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, api.User.UserName),
                new(ClaimTypes.NameIdentifier, api.User.UserId)
            };
            claims.AddRange(api.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            identity = new ClaimsIdentity(claims, "LeaveApi");
        }
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
    private void OnSessionChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    public void Dispose() => api.SessionChanged -= OnSessionChanged;
}