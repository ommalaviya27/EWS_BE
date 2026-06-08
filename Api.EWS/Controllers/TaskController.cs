using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var result = await taskService.GetAllTasksAsync(request, projectId);
            return Ok(ResponseHelper.SuccessResponse(result, "Tasks fetched successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await taskService.GetTaskByIdAsync(id);
            return Ok(ResponseHelper.SuccessResponse(result, "Task fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
        {
            var result = await taskService.CreateTaskAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Task created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request)
        {
            var result = await taskService.UpdateTaskAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Task updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await taskService.DeleteTaskAsync(id);
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Task deleted successfully."));
        }

        [HttpGet("team-members")]
        public async Task<IActionResult> GetTeamMembers()
        {
            var result = await taskService.GetTeamMembersAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Team members fetched successfully."));
        }

        [HttpGet("my-projects")]
        public async Task<IActionResult> GetMyProjects([FromQuery] ProjectListRequest request)
        {
            var result = await taskService.GetMyProjectsAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Projects fetched successfully."));
        }

        [HttpGet("team-lead-dashboard")]
        public async Task<IActionResult> GetTeamLeadDashboard()
        {
            var result = await taskService.GetTeamLeadDashboardAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Team lead dashboard data fetched successfully."));
        }
    }
}