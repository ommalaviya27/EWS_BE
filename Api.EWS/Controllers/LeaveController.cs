using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.DataModel.Request;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/leave")]
    public class LeaveController(ILeaveService leaveService) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await leaveService.GetByIdAsync(id);
            return Ok(ResponseHelper.SuccessResponse(result, "Leave application fetched successfully."));
        }

        [HttpGet("my-leaves")]
        public async Task<IActionResult> GetMyLeaves([FromQuery] PaginationRequest pagination)
        {
            var result = await leaveService.GetMyLeavesAsync(pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Leave applications fetched successfully."));
        }

        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingForReview([FromQuery] PaginationRequest pagination)
        {
            var result = await leaveService.GetPendingForReviewAsync(pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Pending leave applications fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] ApplyLeaveRequest request)
        {
            var result = await leaveService.ApplyAsync(request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Leave application submitted successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] EditLeaveRequest request)
        {
            var result = await leaveService.EditAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Leave application updated successfully."));
        }

        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewLeaveRequest request)
        {
            var result = await leaveService.ReviewAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Leave application reviewed successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await leaveService.DeleteAsync(id);
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Leave application deleted successfully."));
        }
    }
}