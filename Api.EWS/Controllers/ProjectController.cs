using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/projects")]
    public class ProjectController(IProjectService projectService) : ControllerBase
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

        [HttpGet("team-leaders")]
        public async Task<IActionResult> GetTeamLeaders()
        {
            var result = await projectService.GetTeamLeadersAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Team leaders fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
        {
            var result = await projectService.CreateProjectAsync(request, GetCallerRoleId());
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ResponseHelper.SuccessResponse(result, "Project created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request)
        {
            var result = await projectService.UpdateProjectAsync(id, request, GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Project updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await projectService.DeleteProjectAsync(id, GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse<object>(null, "Project deleted successfully."));
        }

        private int GetCallerRoleId()
            => int.TryParse(User.FindFirst("role_id")?.Value, out var roleId) ? roleId : 0;
    }
}