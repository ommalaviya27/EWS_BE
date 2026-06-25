using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
    {
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly([FromQuery] AttendanceMonthRequest request)
        {
            var result = await attendanceService.GetMonthlyAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Monthly attendance fetched successfully."));
        }

        [HttpGet("team-monthly")]
        public async Task<IActionResult> GetTeamMonthly([FromQuery] AttendanceTeamMonthRequest request)
        {
            var result = await attendanceService.GetTeamMonthlyAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Team monthly attendance fetched successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await attendanceService.GetByIdAsync(id);
            return Ok(ResponseHelper.SuccessResponse(result, "Attendance record fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddAttendanceRequest request)
        {
            var result = await attendanceService.AddAsync(request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Attendance submitted successfully.", HttpStatusCode.Created));
        }

        [HttpPost("admin-fill")]
        public async Task<IActionResult> AdminFill([FromBody] AdminAddAttendanceRequest request)
        {
            var result = await attendanceService.AdminAddAsync(request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Attendance filled successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] EditAttendanceRequest request)
        {
            var result = await attendanceService.EditAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Attendance updated successfully."));
        }

        [HttpPost("approve-all-pending")]
        public async Task<IActionResult> ApproveAllPending()
        {
            var approvedCount = await attendanceService.ApproveAllPendingAsync();
            return Ok(ResponseHelper.SuccessResponse(approvedCount, $"Successfully approved {approvedCount} pending attendance records."));
        }
    }
}