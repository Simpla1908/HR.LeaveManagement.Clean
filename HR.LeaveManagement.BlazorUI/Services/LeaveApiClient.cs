using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HR.LeaveManagement.BlazorUI.Models;

namespace HR.LeaveManagement.BlazorUI.Services;

// Une instance par circuit Blazor : le jeton n'est jamais partagé entre utilisateurs.
public sealed class LeaveApiClient(IHttpClientFactory factory)
{
    private string? token;
    public CurrentUser? User { get; private set; }
    public bool IsSignedIn => User is not null;
    public event Action? SessionChanged;

    public async Task LoginAsync(LoginModel model)
    {
        Logout();
        var response = await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", model);
        token = response.Token;
        try
        {
            User = await SendAsync<CurrentUser>(HttpMethod.Get, "api/auth/me");
            SessionChanged?.Invoke();
        }
        catch { Logout(); throw; }
    }
    public void Logout()
    {
        token = null;
        User = null;
        SessionChanged?.Invoke();
    }
    public Task RegisterAsync(RegistrationModel model) => SendAsync(HttpMethod.Post, "api/auth/register", model);
    public Task<List<UserModel>> GetUsersAsync() => SendAsync<List<UserModel>>(HttpMethod.Get, "api/users");
    public Task SaveUserAsync(UserFormModel model)
    {
        if (model.Id.Length == 0)
            return SendAsync(HttpMethod.Post, "api/users", new { model.FirstName, model.LastName, model.UserName, model.Email, model.Role, model.Password });
        return SendAsync(HttpMethod.Put, $"api/users/{Uri.EscapeDataString(model.Id)}",
            new { model.FirstName, model.LastName, model.UserName, model.Email, model.Role });
    }
    public Task DeleteUserAsync(string id) => SendAsync(HttpMethod.Delete, $"api/users/{Uri.EscapeDataString(id)}");
    public Task<List<LeaveTypeModel>> GetTypesAsync() => SendAsync<List<LeaveTypeModel>>(HttpMethod.Get, "api/LeaveTypes");
    public Task CreateTypeAsync(LeaveTypeModel model) => SendAsync(HttpMethod.Post, "api/LeaveTypes", model);
    public Task SaveTypeAsync(LeaveTypeModel model) => SendAsync(model.Id == 0 ? HttpMethod.Post : HttpMethod.Put,
        model.Id == 0 ? "api/LeaveTypes" : $"api/LeaveTypes/{model.Id}", model);
    public Task DeleteTypeAsync(int id) => SendAsync(HttpMethod.Delete, $"api/LeaveTypes/{id}");
    public Task<List<AllocationModel>> GetAllocationsAsync() => SendAsync<List<AllocationModel>>(HttpMethod.Get,
        User?.IsAdministrator == true ? "api/LeaveAllocations" : "api/LeaveAllocations/mine");
    public Task AllocateAsync(int typeId) => SendAsync(HttpMethod.Post, "api/LeaveAllocations", new { leaveTypeId = typeId });
    public Task SaveAllocationAsync(AllocationFormModel model) => SendAsync(HttpMethod.Put, $"api/LeaveAllocations/{model.Id}", model);
    public Task DeleteAllocationAsync(int id) => SendAsync(HttpMethod.Delete, $"api/LeaveAllocations/{id}");
    public Task<List<LeaveRequestModel>> GetRequestsAsync() => SendAsync<List<LeaveRequestModel>>(HttpMethod.Get, "api/LeaveRequests");
    public Task SaveRequestAsync(RequestFormModel model) => SendAsync(model.Id == 0 ? HttpMethod.Post : HttpMethod.Put,
        model.Id == 0 ? "api/LeaveRequests" : $"api/LeaveRequests/{model.Id}", model);
    public Task<LeaveRequestModel> GetRequestAsync(int id) => SendAsync<LeaveRequestModel>(HttpMethod.Get, $"api/LeaveRequests/{id}");
    public Task CancelAsync(int id) => SendAsync(HttpMethod.Put, $"api/LeaveRequests/{id}/cancel");
    public Task DeleteRequestAsync(int id) => SendAsync(HttpMethod.Delete, $"api/LeaveRequests/{id}");
    public Task ApproveAsync(int id, bool approved) => SendAsync(HttpMethod.Put, $"api/LeaveRequests/{id}/approval", new { id, approved });

    private async Task SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendCoreAsync(method, path, body);
    }
    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendCoreAsync(method, path, body);
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("La réponse de l'API est vide.");
    }
    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body)
    {
        using var client = factory.CreateClient("LeaveApi");
        using var request = new HttpRequestMessage(method, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode) return response;
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Votre session a expiré ou vos droits ont changé. Reconnectez-vous.",
            HttpStatusCode.Forbidden => "Vous n'avez pas accès à cette opération.",
            HttpStatusCode.NotFound => "La ressource demandée est introuvable.",
            _ => "L'opération a échoué. Réessayez."
        };
        if (response.StatusCode == HttpStatusCode.Unauthorized) Logout();
        if ((int)response.StatusCode == 400)
        {
            try
            {
                using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (problem.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
                if (problem.RootElement.TryGetProperty("errors", out var errors))
                    message += " " + string.Join(" ", errors.EnumerateObject()
                        .SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()).Distinct());
            }
            catch (JsonException) { }
        }
        response.Dispose();
        throw new InvalidOperationException(message);
    }
}
