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
    [Route("api/tasks")]
    public class TaskController(ITaskService taskService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] TaskSearchRequest request, [FromQuery] Guid? projectId)
        {
            var result = await taskService.GetAllTasksAsync(request, projectId, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Tasks fetched successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await taskService.GetTaskByIdAsync(id, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Task fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            var result = await taskService.CreateTaskAsync(request, GetCallerUserId(), GetCallerRoleId());
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Task created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
        {
            var result = await taskService.UpdateTaskAsync(id, request, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Task updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await taskService.DeleteTaskAsync(id, GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Task deleted successfully."));
        }

        [HttpGet("team-members")]
        public async Task<IActionResult> GetTeamMembers()
        {
            var result = await taskService.GetTeamMembersAsync(GetCallerUserId(), GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Team members fetched successfully."));
        }

        [HttpGet("my-projects")]
        public async Task<IActionResult> GetMyProjects()
        {
            var result = await taskService.GetMyProjectsAsync(GetCallerUserId());
            return Ok(ResponseHelper.SuccessResponse(result, "Projects fetched successfully."));
        }

        private int GetCallerUserId()
            => int.TryParse(User.FindFirst("user_id")?.Value, out var uid) ? uid : 0;

        private int GetCallerRoleId()
            => int.TryParse(User.FindFirst("role_id")?.Value, out var rid) ? rid : 0;
    }
}
