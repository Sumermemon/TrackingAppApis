using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Domain.Common;
using RunningCompetition.Persistence.Context;

namespace RunningCompetition.Persistence.Repositories;

/// <summary>
/// Generic repository implementation backed by EF Core.
/// </summary>
/// <typeparam name="TEntity">Entity type extending <see cref="BaseEntity"/>.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>The underlying DbContext.</summary>
    protected readonly AppDbContext Context;

    /// <summary>The DbSet for <typeparamref name="TEntity"/>.</summary>
    protected readonly DbSet<TEntity> DbSet;

    /// <summary>Initializes a new instance of <see cref="Repository{TEntity}"/>.</summary>
    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<TEntity?> FindFirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => await DbSet.AddRangeAsync(entities, cancellationToken);

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
        => DbSet.Update(entity);

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
        => DbSet.UpdateRange(entities);

    /// <inheritdoc />
    public virtual void Delete(TEntity entity)
        => DbSet.Remove(entity);

    /// <inheritdoc />
    public virtual async Task SoftDeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null) return;
        entity.SoftDelete(deletedById);
        Update(entity);
    }

    /// <inheritdoc />
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await Context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public virtual IQueryable<TEntity> Query()
        => DbSet.AsQueryable();

    /// <inheritdoc />
    public virtual IQueryable<TEntity> QueryWithDeleted()
        => DbSet.IgnoreQueryFilters().AsQueryable();
}
