using Domain.EWS.DataModels.Request.PublicHoliday;
using Domain.EWS.DataModels.Response.PublicHoliday;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IPublicHolidayService : IGenericService<PublicHoliday>
    {
        Task<PagedResponse<HolidayResponse>> GetHolidaysAsync(PaginationRequest pagination);
        Task<HolidayResponse> GetHolidayByIdAsync(int id);
        Task<HolidayResponse> CreateAsync(CreateHolidayRequest request);
        Task<HolidayResponse> UpdateAsync(int id, UpdateHolidayRequest request);
        Task DeleteAsync(int id);
    }
}