using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Application.EWS.Interfaces;
using Shared.EWS.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using Shared.EWS.DataModel.Response;
using Shared.EWS.DataModel.Request;
using Shared.EWS.Extensions;

namespace Application.EWS.Services
{
    public class ProjectService(
        IGenericRepository<Projects> repository,
        EWSDbContext context)
        : GenericService<Projects>(repository), IProjectService
    {
        private readonly EWSDbContext _context = context;

        public async Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(PaginationRequest pagination)
        {
            var query = _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new GetProjectResponse
                {
                    Id            = p.Id,
                    Name          = p.Name,
                    Description   = p.Description,
                    UserId        = p.UserId,
                    ProjectStatus = p.ProjectStatus,
                    StartDate     = p.StartDate,
                    EndDate       = p.EndDate
                })
                .AsNoTracking();

            return await query.ToPagedResponseAsync(pagination);
        }

        public async Task<GetProjectResponse?> GetProjectByIdAsync(Guid id)
        {
            var project = await GetByIdAsync(id);
            return project is null ? null : MapToResponse(project);
        }

        public async Task<GetProjectResponse> CreateProjectAsync(CreateProjectRequest request, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "create");

            if (request.EndDate <= request.StartDate)
                throw new InvalidOperationException("End date must be after start date.");

            await ValidateTeamLeaderAsync(request.UserId);

            bool nameExists = await _context.Projects
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && !p.IsDeleted);

            if (nameExists)
                throw new DuplicateRecordException($"A project named '{request.Name}' already exists.");

            var entity = new Projects
            {
                Id            = Guid.NewGuid(),
                Name          = request.Name.Trim(),
                Description   = request.Description.Trim(),
                UserId        = request.UserId,
                ProjectStatus = request.ProjectStatus,
                StartDate     = request.StartDate,
                EndDate       = request.EndDate,
                CreatedBy     = request.UserId
            };

            var created = await AddAsync(entity);
            return MapToResponse(created);
        }

        public async Task<GetProjectResponse> UpdateProjectAsync(Guid id, UpdateProjectRequest request, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "update");

            if (request.EndDate <= request.StartDate)
                throw new InvalidOperationException("End date must be after start date.");

            var project = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Project with id '{id}' was not found.");

            await ValidateTeamLeaderAsync(request.UserId);

            bool nameExists = await _context.Projects
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower()
                            && !p.IsDeleted
                            && p.Id != id);

            if (nameExists)
                throw new DuplicateRecordException($"A project named '{request.Name}' already exists.");

            project.Name          = request.Name.Trim();
            project.Description   = request.Description.Trim();
            project.UserId        = request.UserId;
            project.ProjectStatus = request.ProjectStatus;
            project.StartDate     = request.StartDate;
            project.EndDate       = request.EndDate;

            var updated = await UpdateAsync(project);
            return MapToResponse(updated);
        }

        public async Task<bool> DeleteProjectAsync(Guid id, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "delete");

            var project = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Project with id '{id}' was not found.");

            return await DeleteAsync(project.Id);
        }

        public async Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync()
        {
            return await _context.Users
                .Where(u => u.RoleId == 2 && !u.IsDeleted)
                .Select(u => new TeamLeaderResponse
                {
                    UserId = u.Id,
                    Name   = u.Name,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        private static void ValidateAdmin(int callerRoleId, string action)
        {
            if (callerRoleId != 1)
                throw new ForbiddenException($"Access denied. Only Admin can {action} a project.");
        }

        private async Task ValidateTeamLeaderAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (user.RoleId == 1)
                throw new InvalidOperationException("Project cannot be assigned to an Admin.");

            if (user.RoleId == 3)
                throw new InvalidOperationException("Project cannot be assigned to an Employee.");

            if (user.RoleId != 2)
                throw new InvalidOperationException("Project can only be assigned to a Team Lead.");
        }

        private static GetProjectResponse MapToResponse(Projects p) => new()
        {
            Id            = p.Id,
            Name          = p.Name,
            Description   = p.Description,
            UserId        = p.UserId,
            ProjectStatus = p.ProjectStatus,
            StartDate     = p.StartDate,
            EndDate       = p.EndDate
        };
    }
}