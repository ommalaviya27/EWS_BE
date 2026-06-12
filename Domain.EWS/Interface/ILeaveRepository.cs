using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface ILeaveRepository : IGenericRepository<LeaveApplication>
    {
        Task<LeaveApplication?> GetWithDetailsAsync(int id);
        Task<PagedResponse<LeaveApplication>> GetMyLeavesAsync(int userId, PaginationRequest pagination);
        Task<PagedResponse<LeaveApplication>> GetPendingForReviewAsync(int reviewerId, bool isAdmin, PaginationRequest pagination);
        Task<bool> HasOverlapAsync(int userId, DateTime startDate, DateTime endDate, int? excludeId = null);
        Task<List<int>> GetTeamMemberIdsAsync(int teamLeadId);
    }
}