using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
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

        public async Task<List<Attendance>> GetMonthlyBatchAsync(List<int> userIds, int month, int year)
        {
            var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = first.AddMonths(1);

            return await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Reviewer)
                .Where(a => userIds.Contains(a.UserId)
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

        public async Task<List<int>> GetTeamMemberIdsAsync(int reportingId)
        {
            return await _context.Users
                .Where(u => u.ReportingId == reportingId && u.status && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();
        }

        public async Task<DateTime?> GetUserJoinDateAsync(int userId)
        {
            var createdAt = await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => (DateTime?)u.CreatedAt)
                .FirstOrDefaultAsync();

            return createdAt.HasValue
                ? DateTime.SpecifyKind(createdAt.Value.Date, DateTimeKind.Utc)
                : null;
        }

        public async Task<Dictionary<int, DateTime>> GetUserJoinDatesAsync(List<int> userIds)
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .Select(u => new { u.Id, u.CreatedAt })
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => DateTime.SpecifyKind(u.CreatedAt.Date, DateTimeKind.Utc));
        }

        public async Task<bool> IsPublicHolidayAsync(DateTime date)
        {
            var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            return await _context.PublicHolidays
                .AnyAsync(h => h.HolidayDate >= dayStart && h.HolidayDate < dayEnd && !h.IsDeleted);
        }

        public async Task<List<Attendance>> GetAllPendingForReviewAsync(int reviewerId, bool isAdmin)
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
                    .Where(u => u.ReportingId == reviewerId && u.status && !u.IsDeleted)
                    .Select(u => u.Id);
            }

            return await _context.Attendances
                .Where(a => subjectUserIds.Contains(a.UserId)
                         && a.ApprovalStatus == ApprovalStatus.Pending
                         && !a.IsDeleted)
                .ToListAsync();
        }
    }
}