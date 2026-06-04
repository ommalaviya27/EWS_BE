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
        public async Task<IActionResult> GetEmployeePerformanceReport([FromQuery] EmployeePerformanceReportRequest request)
        {
            var result = await reportService.GetEmployeePerformanceReportAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Employee performance report fetched successfully."));
        }

        [HttpGet("employee-summary")]
        public async Task<IActionResult> GetEmployeeSummary([FromQuery] EmployeeSummaryRequest request)
        {
            var result = await reportService.GetEmployeeSummaryAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Employee summary fetched successfully."));
        }

        [HttpGet("task-completion-overview")]
        public async Task<IActionResult> GetTaskCompletionOverview([FromQuery] TaskCompletionReportRequest request)
        {
            var result = await reportService.GetTaskCompletionOverviewAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Task completion overview fetched successfully."));
        }

        [HttpGet("task-completion-summary")]
        public async Task<IActionResult> GetTaskCompletionSummary([FromQuery] TaskCompletionSummaryRequest request)
        {
            var result = await reportService.GetTaskCompletionSummaryAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Task completion summary fetched successfully."));
        }

        [HttpGet("project-progress-overview")]
        public async Task<IActionResult> GetProjectProgressOverview()
        {
            var result = await reportService.GetProjectProgressOverviewAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Project progress overview fetched successfully."));
        }

        [HttpGet("project-progress-summary")]
        public async Task<IActionResult> GetProjectProgressSummary([FromQuery] ProjectProgressRequest request)
        {
            var result = await reportService.GetProjectProgressSummaryAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Project progress summary fetched successfully."));
        }
    }
}