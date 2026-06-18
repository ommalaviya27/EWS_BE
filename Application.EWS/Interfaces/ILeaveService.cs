using Domain.EWS.DataModels.Request.Leave;
using Domain.EWS.DataModels.Response.Leave;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface ILeaveService : IGenericService<LeaveApplication>
    {
        Task<LeaveResponse> GetByIdAsync(int id);
        Task<PagedResponse<LeaveResponse>> GetMyLeavesAsync(PaginationRequest pagination);
        Task<PagedResponse<LeaveResponse>> GetPendingForReviewAsync(PaginationRequest pagination);
        Task<LeaveResponse> ApplyAsync(ApplyLeaveRequest request);
        Task<LeaveResponse> EditAsync(int id, EditLeaveRequest request);
        Task<LeaveResponse> ReviewAsync(int id, ReviewLeaveRequest request);
        Task DeleteAsync(int id);
    }
}