using Domain.EWS.DataModels.Request.Attendance;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IAttendanceRepository : IGenericRepository<Attendance>
    {
        Task<Attendance?> GetWithDetailsAsync(int id);

        Task<PagedResponse<Attendance>> GetAllWithDetailsAsync(AttendanceSearchRequest request,int? forceUserId = null,List<int>? teamMemberIds = null);

        Task<List<Attendance>> GetMonthlyAsync(int userId, int month, int year);

        Task<bool> ExistsForDateAsync(int userId, DateTime date, int? excludeId = null);

        Task<List<int>> GetTeamMemberIdsAsync(int teamLeadId);
    }
}
