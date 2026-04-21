using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common.Models;

namespace SchoolManagement.Persistence.Common;

internal static class QueryableExtensions
{
    public static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TEntity, TResult>(
        this IQueryable<TEntity> query,
        PaginationRequest request,
        CancellationToken cancellationToken,
        Func<TEntity, TResult> selector)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResponse<TResult>
        {
            Items = items.Select(selector).ToArray(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
