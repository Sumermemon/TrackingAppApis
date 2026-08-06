using System.Linq.Expressions;
using RunningCompetition.Domain.Common;

namespace RunningCompetition.Contracts.Repositories;

/// <summary>
/// Generic repository interface for basic CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The entity type (must extend <see cref="BaseEntity"/>).</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>Gets an entity by its primary key.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all entities (excluding soft-deleted).</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds entities matching a predicate.</summary>
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Finds a single entity matching a predicate.</summary>
    Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Checks whether any entity matches a predicate.</summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching a predicate.</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity.</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Adds a range of new entities.</summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entity.</summary>
    void Update(TEntity entity);

    /// <summary>Updates a range of entities.</summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>Hard deletes an entity.</summary>
    void Delete(TEntity entity);

    /// <summary>Soft deletes an entity by ID.</summary>
    Task SoftDeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);

    /// <summary>Saves all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns an <see cref="IQueryable{T}"/> for complex queries.</summary>
    IQueryable<TEntity> Query();

    /// <summary>Returns an <see cref="IQueryable{T}"/> including soft-deleted entities.</summary>
    IQueryable<TEntity> QueryWithDeleted();
}
