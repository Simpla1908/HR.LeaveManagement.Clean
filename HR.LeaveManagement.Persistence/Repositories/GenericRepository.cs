
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Domain.Common;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Persistence.Repositories;

public class GenericRepository<T> : IGenericRepository<T>
    where T : BaseEntity
{
    protected readonly HrDatabaseContext _context;

    public GenericRepository(HrDatabaseContext context)
    {
        _context = context;
    }

    // Récupérer toutes les entités.
    public async Task<IReadOnlyList<T>> GetAsync()
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .ToListAsync();
    }

    // Récupérer une entité par son identifiant.
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    // Créer une nouvelle entité.
    public async Task CreateAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);

        await _context.SaveChangesAsync();
    }

    // Modifier une entité.
    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);

        await _context.SaveChangesAsync();
    }

    // Supprimer une entité.
    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);

        await _context.SaveChangesAsync();
    }
}
