using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IAttendanceRepository : IGenericRepository<Attendance>
    {
        Task<Attendance?> GetWithDetailsAsync(int id);
        Task<List<Attendance>> GetMonthlyAsync(int userId, int month, int year);
        Task<bool> ExistsForDateAsync(int userId, DateTime date, int? excludeId = null);
        Task<List<int>> GetTeamMemberIdsAsync(int teamLeadId);
        Task<PagedResponse<Attendance>> GetPendingForReviewAsync(int reviewerId, bool isAdmin, PaginationRequest pagination);
        Task<DateTime?> GetUserJoinDateAsync(int userId);
        Task<bool> IsPublicHolidayAsync(DateTime date);
    }
}