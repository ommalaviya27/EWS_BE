using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.DataModels.Response.Attendance;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IAttendanceService : IGenericService<Attendance>
    {
        Task<AttendanceResponse> GetByIdAsync(int id);
        Task<AttendanceMonthResponse> GetMonthlyAsync(AttendanceMonthRequest request);
        Task<AttendanceResponse> AddAsync(AddAttendanceRequest request);
        Task<AttendanceResponse> AdminAddAsync(AdminAddAttendanceRequest request);
        Task<AttendanceResponse> EditAsync(int id, EditAttendanceRequest request);
        Task<AttendanceResponse> ReviewAsync(int id, ReviewAttendanceRequest request);
        Task<PagedResponse<AttendanceResponse>> GetPendingForReviewAsync(PaginationRequest pagination);
    }
}
