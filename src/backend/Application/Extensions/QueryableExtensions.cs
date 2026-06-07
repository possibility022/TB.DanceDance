using Microsoft.EntityFrameworkCore;

namespace Application.Extensions;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public async Task<(IReadOnlyCollection<T> Items, int TotalCount)> ToPagedResultAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArrayAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
