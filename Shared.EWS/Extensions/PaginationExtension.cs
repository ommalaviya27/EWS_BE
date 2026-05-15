using Microsoft.EntityFrameworkCore;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;

namespace Shared.EWS.Extensions
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query,
            PaginationRequest pagination)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return PagedResponse<T>.Create(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
