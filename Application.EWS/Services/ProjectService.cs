using AutoMapper;
using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.Interface;
using Application.EWS.Interfaces;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class ProjectService(
        IProjectRepository repository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Projects>(repository, principal), IProjectService
    {
        private readonly IProjectRepository _projectRepository = repository;
        private readonly IMapper _mapper = mapper;

        public async Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(ProjectSearchRequest request)
            => await _projectRepository.GetAllProjectsAsync(request);

        public async Task<GetProjectResponse?> GetProjectByIdAsync(Guid id)
        {
            var project = await GetByIdAsync(id);
            return project is null ? null : _mapper.Map<GetProjectResponse>(project);
        }

        public async Task<GetProjectResponse> CreateProjectAsync(CreateProjectRequest request)
        {
            ValidateAdmin("create");

            if (request.EndDate <= request.StartDate)
                throw new InvalidOperationException("End date must be after start date.");

            await ValidateTeamLeaderAsync(request.UserId);

            if (await _projectRepository.ProjectNameExistsAsync(request.Name))
                throw new DuplicateRecordException($"A project named '{request.Name}' already exists.");

            var entity = new Projects
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                UserId = request.UserId,
                ProjectStatus = request.ProjectStatus,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedBy = request.UserId
            };

            var created = await AddAsync(entity);
            return _mapper.Map<GetProjectResponse>(created);
        }

        public async Task<GetProjectResponse> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
        {
            ValidateAdmin("update");

            if (request.EndDate <= request.StartDate)
                throw new InvalidOperationException("End date must be after start date.");

            var project = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Project with id '{id}' was not found.");

            await ValidateTeamLeaderAsync(request.UserId);

            if (await _projectRepository.ProjectNameExistsAsync(request.Name, id))
                throw new DuplicateRecordException($"A project named '{request.Name}' already exists.");

            project.Name = request.Name.Trim();
            project.Description = request.Description.Trim();
            project.UserId = request.UserId;
            project.ProjectStatus = request.ProjectStatus;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;

            var updated = await UpdateAsync(project);
            return _mapper.Map<GetProjectResponse>(updated);
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            ValidateAdmin("delete");

            var project = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Project with id '{id}' was not found.");

            return await DeleteAsync(project.Id);
        }

        public async Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync()
            => await _projectRepository.GetTeamLeadersAsync();

        private void ValidateAdmin(string action)
        {
            if (CurrentRoleId != 1)
                throw new ForbiddenException($"Access denied. Only Admin can {action} a project.");
        }

        private async Task ValidateTeamLeaderAsync(int userId)
        {
            var user = await _projectRepository.GetUserWithRoleAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (user.RoleId == 1)
                throw new InvalidOperationException("Project cannot be assigned to an Admin.");

            if (user.RoleId == 3)
                throw new InvalidOperationException("Project cannot be assigned to an Employee.");

            if (user.RoleId != 2)
                throw new InvalidOperationException("Project can only be assigned to a Team Lead.");
        }
    }
}