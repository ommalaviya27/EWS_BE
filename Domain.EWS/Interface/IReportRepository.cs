using Domain.EWS.DataModels.Request.Reports;
using Domain.EWS.DataModels.Response.Reports;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IReportRepository : IGenericRepository<User>
    {
        Task<EmployeePerformanceReportResponse> GetEmployeePerformanceReportAsync(EmployeePerformanceReportRequest request);
        Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(EmployeeSummaryRequest request);
    }
}