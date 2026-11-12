using System.Data;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Persistence;

public sealed class UnitOfWork(HrDatabaseContext context) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Reload all state on retries, including after a SQL Server deadlock.
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                context.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
