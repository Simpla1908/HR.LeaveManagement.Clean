using HR.LeaveManagement.BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace HR.LeaveManagement.BlazorUI.Components.Shared;

// État local d'une page : chargement, succès et erreurs. L'API reste l'autorité sur les droits.
public abstract class ApiPageBase : ComponentBase
{
    [Inject] protected LeaveApiClient Api { get; set; } = default!;
    protected bool IsBusy { get; private set; }
    protected string? Error { get; private set; }
    protected string? Success { get; private set; }
    protected virtual Task LoadAsync() => Task.CompletedTask;
    protected override async Task OnInitializedAsync()
    {
        if (Api.IsSignedIn) await RunAsync(LoadAsync);
    }
    protected async Task RunAsync(Func<Task> action, string? success = null)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = Success = null;
        try { await action(); Success = success; }
        catch (HttpRequestException) { Error = "Le serveur est inaccessible. Vérifiez que l'API est démarrée, puis réessayez."; }
        catch (TaskCanceledException) { Error = "Le serveur met trop de temps à répondre. Réessayez."; }
        catch (InvalidOperationException exception) { Error = exception.Message; }
        finally { IsBusy = false; }
    }
}