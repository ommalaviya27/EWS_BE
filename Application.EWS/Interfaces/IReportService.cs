using Domain.EWS.DataModels.Request.Reports;
using Domain.EWS.DataModels.Response.Reports;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IReportService : IGenericService<User>
    {
        Task<EmployeePerformanceReportResponse> GetEmployeePerformanceReportAsync(EmployeePerformanceReportRequest request);
        Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(EmployeeSummaryRequest request);
    }
}