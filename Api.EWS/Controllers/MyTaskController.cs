using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.DataModel.Request;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/my-tasks")]
    public class MyTaskController(IMyTaskService myTaskService) : ControllerBase
    {
        //  Employee (& above) 
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks([FromQuery] Guid? projectId)
        {
            var result = await myTaskService.GetMyTasksAsync(GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "My tasks fetched successfully."));
        }

        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddTaskCommentRequest request)
        {
            var result = await myTaskService.AddCommentAsync(id, request, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Comment added successfully."));
        }

        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var result = await myTaskService.GetCommentsAsync(id, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Comments fetched successfully."));
        }

        [HttpPost("{id:int}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddAttachments(int id, [FromForm] List<IFormFile> files)
        {
            var result = await myTaskService.AddAttachmentsAsync(id, files, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Attachments uploaded successfully."));
        }

        // Helpers
        private int GetCallerUserId()
            => int.TryParse(User.FindFirst("user_id")?.Value, out var uid) ? uid : 0;

        private int GetCallerRoleId()
            => int.TryParse(User.FindFirst("role_id")?.Value, out var rid) ? rid : 0;
    }
}