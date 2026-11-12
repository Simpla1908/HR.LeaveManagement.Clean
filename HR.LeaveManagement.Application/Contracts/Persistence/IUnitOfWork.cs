namespace HR.LeaveManagement.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
