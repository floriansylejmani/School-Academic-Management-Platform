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
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResponse<TResult>
        {
            Items = items.Select(selector).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
