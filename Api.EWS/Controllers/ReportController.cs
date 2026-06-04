using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/reports")]
    public class ReportController(IReportService reportService) : ControllerBase
    {
        [HttpGet("employee-performance")]
        public async Task<IActionResult> GetEmployeePerformanceReport( [FromQuery] EmployeePerformanceReportRequest request)
        {
            var result = await reportService.GetEmployeePerformanceReportAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Employee performance report fetched successfully."));
        }

        [HttpGet("employee-summary")]
        public async Task<IActionResult> GetEmployeeSummary( [FromQuery] EmployeeSummaryRequest request)
        {
            var result = await reportService.GetEmployeeSummaryAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Employee summary fetched successfully."));
        }
    }
}