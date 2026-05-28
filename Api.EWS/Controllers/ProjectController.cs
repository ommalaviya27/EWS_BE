using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Request.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/projects")]
    public class ProjectController(IProjectService projectService, ITaskService taskService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProjectSearchRequest request)
        {
            var result = await projectService.GetAllProjectsAsync(request);
            return Ok(ResponseHelper.SuccessResponse(result, "Projects fetched successfully."));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await projectService.GetProjectByIdAsync(id);
            return Ok(ResponseHelper.SuccessResponse(result, "Project fetched successfully."));
        }

        [HttpGet("{id:guid}/tasks")]
        public async Task<IActionResult> GetProjectTasks(Guid id, [FromQuery] TaskSearchRequest request)
        {
            var result = await taskService.GetAllTasksAsync(request, id);
            return Ok(ResponseHelper.SuccessResponse(result, "Project tasks fetched successfully."));
        }

        [HttpGet("team-leaders")]
        public async Task<IActionResult> GetTeamLeaders()
        {
            var result = await projectService.GetTeamLeadersAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Team leaders fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
        {
            var result = await projectService.CreateProjectAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Project created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request)
        {
            var result = await projectService.UpdateProjectAsync(id, request);
            return Ok(ResponseHelper.SuccessResponse(result, "Project updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await projectService.DeleteProjectAsync(id);
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Project deleted successfully."));
        }
    }
}