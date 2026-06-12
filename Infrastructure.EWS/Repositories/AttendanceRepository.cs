using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

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

        public async Task<PagedResponse<Attendance>> GetPendingForReviewAsync(int reviewerId, bool isAdmin, PaginationRequest pagination)
        {
            IQueryable<int> subjectUserIds;

            if (isAdmin)
            {
                subjectUserIds = _context.Users
                    .Where(u => u.RoleId == 2 && u.status && !u.IsDeleted)
                    .Select(u => u.Id);
            }
            else
            {
                subjectUserIds = _context.Users
                    .Where(u => u.TeamLeadId == reviewerId && u.status && !u.IsDeleted)
                    .Select(u => u.Id);
            }

            var query = _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => subjectUserIds.Contains(a.UserId)
                         && a.ApprovalStatus == ApprovalStatus.Pending
                         && !a.IsDeleted)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.UserId)
                .AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return PagedResponse<Attendance>.Create(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}