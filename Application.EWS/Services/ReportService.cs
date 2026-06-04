using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Reports;
using Domain.EWS.DataModels.Response.Reports;
using Domain.EWS.Interface;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class ReportService(IReportRepository repository, ClaimsPrincipal principal)
        : GenericService<User>(repository, principal), IReportService
    {
        private IReportRepository ReportRepository => (IReportRepository)_repository;

        private static readonly string[] AllowedFilters = ["weekly", "monthly"];

        public async Task<EmployeePerformanceReportResponse> GetEmployeePerformanceReportAsync(EmployeePerformanceReportRequest request)
        {
            EnsureAdmin();

            if (string.IsNullOrWhiteSpace(request.Filter) ||
                !AllowedFilters.Contains(request.Filter.ToLower()))
                request.Filter = "monthly";

            return await ReportRepository.GetEmployeePerformanceReportAsync(request);
        }

        public async Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(EmployeeSummaryRequest request)
        {
            EnsureAdmin();
            return await ReportRepository.GetEmployeeSummaryAsync(request);
        }

        private void EnsureAdmin()
        {
            if (CurrentRoleId != 1)
                throw new ForbiddenException("Access denied. Only Admin can access reports.");
        }
    }
}