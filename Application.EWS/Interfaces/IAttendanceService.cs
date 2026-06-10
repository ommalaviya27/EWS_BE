using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.DataModels.Response.Attendance;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IAttendanceService : IGenericService<Attendance>
    {
        Task<PagedResponse<AttendanceResponse>> GetAllAsync(AttendanceSearchRequest request);
        Task<AttendanceResponse> GetByIdAsync(int id);
        Task<AttendanceMonthResponse> GetMonthlyAsync(AttendanceMonthRequest request);
        Task<AttendanceResponse> AddAsync(AddAttendanceRequest request);
        Task<AttendanceResponse> EditAsync(int id, EditAttendanceRequest request);
        Task<AttendanceResponse> ReviewAsync(int id, ReviewAttendanceRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
