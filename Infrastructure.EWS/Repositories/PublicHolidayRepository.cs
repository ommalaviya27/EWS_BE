using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;

namespace Infrastructure.EWS.Repositories
{
    public class PublicHolidayRepository(EWSDbContext context)
        : GenericRepository<PublicHoliday>(context), IPublicHolidayRepository
    {
        public async Task<bool> ExistsForDateAsync(DateTime date, int? excludeId = null)
        {
            var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            var query = _context.PublicHolidays
                .Where(h => h.HolidayDate >= dayStart && h.HolidayDate < dayEnd && !h.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(h => h.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<PagedResponse<PublicHoliday>> GetPagedAsync(PaginationRequest pagination)
        {
            var query = _context.PublicHolidays
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.HolidayDate)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return PagedResponse<PublicHoliday>.Create(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<PublicHoliday>> GetHolidaysForMonthAsync(int month, int year)
        {
            var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = first.AddMonths(1);

            return await _context.PublicHolidays
                .Where(h => h.HolidayDate >= first && h.HolidayDate < last && !h.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}