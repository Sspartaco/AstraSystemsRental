using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Base.Persistence;

public abstract class BaseRepository<TContext, T>(TContext context) : IBaseRepository<T>
    where TContext : DbContext
    where T : class
{
    protected readonly TContext DbContext = context;
    protected readonly DbSet<T> EntitySet = context.Set<T>();

    public virtual IQueryable<T> Query() => EntitySet.AsNoTracking();

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await EntitySet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await EntitySet.FindAsync([id], cancellationToken);

    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await EntitySet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await EntitySet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task<List<T>> GetWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = includes.Aggregate<Expression<Func<T, object>>, IQueryable<T>>(EntitySet, (current, include) => current.Include(include));
        if (includes.Length > 1)
            query = query.AsSplitQuery();
        return await query.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> GetEntityWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = includes.Aggregate<Expression<Func<T, object>>, IQueryable<T>>(EntitySet, (current, include) => current.Include(include));
        return await query.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await EntitySet.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await EntitySet.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => EntitySet.Update(entity);

    public virtual void Remove(T entity) => EntitySet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities) => EntitySet.RemoveRange(entities);

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await EntitySet.AnyAsync(predicate, cancellationToken);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate is null
            ? await EntitySet.CountAsync(cancellationToken)
            : await EntitySet.CountAsync(predicate, cancellationToken);

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await DbContext.SaveChangesAsync(cancellationToken);

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber, int pageSize, Func<IQueryable<T>, IQueryable<T>>? queryBuilder = null, CancellationToken cancellationToken = default)
    {
        var (page, size) = Normalize(pageNumber, pageSize);
        var query = queryBuilder is null ? EntitySet.AsNoTracking() : queryBuilder(EntitySet.AsNoTracking());

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, page, size);
    }

    public virtual async Task<PagedResult<TResult>> GetPagedAsync<TResult>(
        int pageNumber, int pageSize, Func<IQueryable<T>, IQueryable<TResult>> projection,
        Func<IQueryable<T>, IQueryable<T>>? queryBuilder = null, CancellationToken cancellationToken = default)
    {
        var (page, size) = Normalize(pageNumber, pageSize);
        var baseQuery = queryBuilder is null ? EntitySet.AsNoTracking() : queryBuilder(EntitySet.AsNoTracking());

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await projection(baseQuery).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);

        return new PagedResult<TResult>(items, totalCount, page, size);
    }

    private static (int Page, int Size) Normalize(int pageNumber, int pageSize)
        => (pageNumber < 1 ? 1 : pageNumber, pageSize is < 1 or > 200 ? 20 : pageSize);
}
