using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;

namespace Infrastructure.EWS.Repositories
{
    public class AttendanceRepository(EWSDbContext context)
        : GenericRepository<Attendance>(context), IAttendanceRepository
    {
        public async Task<Attendance?> GetWithDetailsAsync(int id)
        {
            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => a.Id == id && !a.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResponse<Attendance>> GetAllWithDetailsAsync(AttendanceSearchRequest request,int? forceUserId = null,List<int>? teamMemberIds = null)
        {
            var query = _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => !a.IsDeleted)
                .AsNoTracking();

            if (forceUserId.HasValue)
                query = query.Where(a => a.UserId == forceUserId.Value);
            else if (teamMemberIds is { Count: > 0 })
                query = query.Where(a => teamMemberIds.Contains(a.UserId));

            if (request.UserId.HasValue)
                query = query.Where(a => a.UserId == request.UserId.Value);

            if (request.Month.HasValue && request.Year.HasValue)
            {
                var first = new DateTime(request.Year.Value, request.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var last = first.AddMonths(1);
                query = query.Where(a => a.AttendanceDate >= first && a.AttendanceDate < last);
            }
            else if (request.Month.HasValue)
            {
                query = query.Where(a => a.AttendanceDate.Month == request.Month.Value);
            }

            if (request.Status.HasValue)
                query = query.Where(a => a.Status == request.Status.Value);

            if (request.ApprovalStatus.HasValue)
                query = query.Where(a => a.ApprovalStatus == request.ApprovalStatus.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.UserId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<Attendance>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<List<Attendance>> GetMonthlyAsync(int userId, int month, int year)
        {
            var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = first.AddMonths(1);

            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => a.UserId == userId
                         && a.AttendanceDate >= first
                         && a.AttendanceDate < last
                         && !a.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ExistsForDateAsync(int userId, DateTime date, int? excludeId = null)
        {
            var dayStart = date.Date.ToUniversalTime();
            var dayEnd = dayStart.AddDays(1);

            var query = _context.Attendances
                .Where(a => a.UserId == userId
                         && a.AttendanceDate >= dayStart
                         && a.AttendanceDate < dayEnd
                         && !a.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<List<int>> GetTeamMemberIdsAsync(int teamLeadId)
        {
            return await _context.Users
                .Where(u => u.TeamLeadId == teamLeadId && u.status && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();
        }
    }
}