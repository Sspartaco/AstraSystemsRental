using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AstraSystemsRental.Base.Persistence;

namespace AstraSystemsRental.Base.Mapping;

public static class MappingExtensions
{
    public static async Task<PagedResult<TResult>> ProjectPaged<TSource, TResult>(
        this IQueryable<TSource> query,
        int pageNumber,
        int pageSize,
        Expression<Func<TSource, TResult>> projection,
        CancellationToken cancellationToken = default)
    {
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize is < 1 or > 200 ? 20 : pageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>(items, totalCount, page, size);
    }
}
