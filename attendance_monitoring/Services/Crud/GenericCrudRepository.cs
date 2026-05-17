using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// Generic EF Core repository implementation for CRUD operations.
/// Uses ApplicationDbContext directly with DbSet access.
/// </summary>
public class GenericCrudRepository<TEntity> : IGenericCrudRepository<TEntity>
    where TEntity : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericCrudRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync().ConfigureAwait(false);
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<TEntity?> GetByIdTrackedAsync(Guid id)
    {
        // FindAsync uses the change tracker by default, so this returns a tracked entity
        return await _dbSet.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<TEntity> CreateAsync(TEntity entity)
    {
        var entry = await _dbSet.AddAsync(entity).ConfigureAwait(false);
        return entry.Entity;
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
