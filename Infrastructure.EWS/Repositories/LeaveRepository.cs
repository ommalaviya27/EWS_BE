using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

namespace Infrastructure.EWS.Repositories
{
    public class LeaveRepository(EWSDbContext context)
        : GenericRepository<LeaveApplication>(context), ILeaveRepository
    {
        public async Task<LeaveApplication?> GetWithDetailsAsync(int id)
        {
            return await _context.LeaveApplications
                .Include(l => l.User)
                .Include(l => l.Reviewer)
                .Where(l => l.Id == id && !l.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResponse<LeaveApplication>> GetMyLeavesAsync(int userId, PaginationRequest pagination)
        {
            var query = _context.LeaveApplications
                .Include(l => l.User)
                .Include(l => l.Reviewer)
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .OrderByDescending(l => l.StartDate)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return PagedResponse<LeaveApplication>.Create(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PagedResponse<LeaveApplication>> GetPendingForReviewAsync(int reviewerId, bool isAdmin, PaginationRequest pagination)
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

            var query = _context.LeaveApplications
                .Include(l => l.User)
                .Include(l => l.Reviewer)
                .Where(l => subjectUserIds.Contains(l.UserId)
                         && l.LeaveStatus == ApprovalStatus.Pending
                         && !l.IsDeleted)
                .OrderByDescending(l => l.StartDate)
                .ThenBy(l => l.UserId)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return PagedResponse<LeaveApplication>.Create(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<bool> HasOverlapAsync(int userId, DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var query = _context.LeaveApplications
                .Where(l => l.UserId == userId
                         && !l.IsDeleted
                         && l.LeaveStatus != ApprovalStatus.Rejected
                         && l.StartDate <= endDate
                         && l.EndDate >= startDate);

            if (excludeId.HasValue)
                query = query.Where(l => l.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<List<int>> GetTeamMemberIdsAsync(int reportingId)
        {
            return await _context.Users
                .Where(u => u.ReportingId == reportingId && u.status && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetAutoPlacedAttendancesAsync(
            int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Attendances
                .Where(a => a.UserId == userId
                         && a.AttendanceDate >= startDate
                         && a.AttendanceDate <= endDate
                         && !a.IsDeleted)
                .ToListAsync();
        }

        public async Task<Attendance?> GetForDateIncludingDeletedAsync(int userId, DateTime date)
        {
            var dayStart = date.Date.ToUniversalTime();
            var dayEnd = dayStart.AddDays(1);

            return await _context.Attendances
                .Where(a => a.UserId == userId
                         && a.AttendanceDate >= dayStart
                         && a.AttendanceDate < dayEnd)
                .FirstOrDefaultAsync();
        }
    }
}