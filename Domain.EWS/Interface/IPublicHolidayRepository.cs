using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IPublicHolidayRepository : IGenericRepository<PublicHoliday>
    {
        Task<bool> ExistsForDateAsync(DateTime date, int? excludeId = null);
        Task<PagedResponse<PublicHoliday>> GetPagedAsync(PaginationRequest pagination);
        Task<List<PublicHoliday>> GetHolidaysForMonthAsync(int month, int year);
    }
}