using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.MyTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/my-tasks")]
    public class MyTaskController(IMyTaskService myTaskService) : ControllerBase
    {
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await myTaskService.GetEmployeeDashboardAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Dashboard data fetched successfully."));
        }

        [HttpGet("my-projects")]
        public async Task<IActionResult> GetMyProjects()
        {
            var result = await myTaskService.GetMyProjectsAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "My projects fetched successfully."));
        }

        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks([FromQuery] MyTaskSearchRequest request, [FromQuery] Guid? projectId)
        {
            var result = await myTaskService.GetMyTasksAsync(request, projectId);
            return Ok(ResponseHelper.SuccessResponse(result, "My tasks fetched successfully."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            var result = await myTaskService.UpdateTaskStatusAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Task status updated successfully."));
        }

        [HttpPost("{id:int}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddTaskCommentRequest request)
        {
            var result = await myTaskService.AddCommentAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Comment added successfully."));
        }

        [HttpPut("{commentId:int}/comments")]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateTaskCommentRequest request)
        {
            var result = await myTaskService.UpdateCommentAsync(commentId, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Comment updated successfully."));
        }

        [HttpDelete("{commentId:int}/comments")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            await myTaskService.DeleteCommentAsync(commentId);
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Comment deleted successfully."));
        }

        [HttpGet("{id:int}/comments")]
        public async Task<IActionResult> GetComments(int id, [FromQuery] GetCommentPaginationRequest pagination)
        {
            var result = await myTaskService.GetCommentsPagedAsync(id, pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Comments fetched successfully."));
        }

        [HttpPost("{id:int}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddAttachments(int id, [FromForm] List<IFormFile> files)
        {
            var result = await myTaskService.AddAttachmentsAsync(id, files);
            return Ok(ResponseHelper.SuccessResponse(result, "Attachments uploaded successfully."));
        }

        [HttpDelete("{attachmentId:int}/attachments")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            await myTaskService.DeleteAttachmentAsync(attachmentId);
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Attachment deleted successfully."));
        }

        [HttpGet("{id:int}/attachments")]
        public async Task<IActionResult> GetAttachments(int id, [FromQuery] GetAttachmentPaginationRequest pagination)
        {
            var result = await myTaskService.GetAttachmentsPagedAsync(id, pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Attachments fetched successfully."));
        }

        [HttpGet("attachments/{attachmentId:int}/download")]
        public async Task<IResult> DownloadAttachment(int attachmentId)
        {
            var result = await myTaskService.DownloadAttachmentAsync(attachmentId);
            return result;
        }
    }
}